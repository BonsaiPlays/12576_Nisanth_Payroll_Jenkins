import {
  Box,
  Button,
  Card,
  CardContent,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Typography,
} from "@mui/material";
import {
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LineElement,
  LinearScale,
  PointElement,
  Title,
  Tooltip,
} from "chart.js";
import React, { useEffect, useState, useContext } from "react";
import { Line } from "react-chartjs-2";
import api from "../../api/axiosClient";
import { SnackbarContext } from "../../context/SnackbarProvider"; // ✅ global snackbar

ChartJS.register(
  CategoryScale,
  LinearScale,
  LineElement,
  PointElement,
  Title,
  Tooltip,
  Legend
);

function Compare() {
  const [viewType, setViewType] = useState("payslips");
  const [list, setList] = useState([]);
  const [selectA, setSelectA] = useState("");
  const [selectB, setSelectB] = useState("");
  const [detailA, setDetailA] = useState(null);
  const [detailB, setDetailB] = useState(null);
  const [compare, setCompare] = useState(null);

  const showSnackbar = useContext(SnackbarContext);

  useEffect(() => {
    loadList();
  }, [viewType]);

  const loadList = async () => {
    try {
      if (viewType === "payslips") {
        const { data } = await api.get(
          "/employee/payslips?page=1&pageSize=100"
        );
        setList(data.items || []);
      } else {
        const { data } = await api.get("/employee/ctcs");
        setList(data || []);
      }
    } catch {
      setList([]);
      showSnackbar("Failed to load records", "error");
    }
    setSelectA("");
    setSelectB("");
    setCompare(null);
    setDetailA(null);
    setDetailB(null);
  };

  const loadDetail = async (id, pos) => {
    try {
      if (viewType === "payslips") {
        const { data } = await api.get(`/employee/payslips/${id}`);
        pos === "A" ? setDetailA(data) : setDetailB(data);
      } else {
        const record = list.find((x) => x.id === id);
        pos === "A" ? setDetailA(record) : setDetailB(record);
      }
    } catch {
      showSnackbar("Failed to load detail", "error");
      pos === "A" ? setDetailA(null) : setDetailB(null);
    }
  };

  const totalAllowances = (d) =>
    (d?.allowanceItems || d?.allowances || []).reduce(
      (sum, a) => sum + a.amount,
      0
    );
  const totalDeductions = (d) =>
    (d?.deductionItems || d?.deductions || []).reduce(
      (sum, a) => sum + a.amount,
      0
    );

  const doCompare = () => {
    if (!detailA || !detailB) {
      showSnackbar("Select both records first", "warning");
      return;
    }

    const diff = {};

    ["basic", "grossCTC", "netPay", "taxDeducted", "taxPercent"].forEach(
      (f) => {
        const a = detailA[f] || 0;
        const b = detailB[f] || 0;
        if (a !== b)
          diff[f] = {
            a,
            b,
            delta: b - a,
            percent: a !== 0 ? (((b - a) / a) * 100).toFixed(1) + "%" : "-",
          };
      }
    );

    const aAllow = totalAllowances(detailA);
    const bAllow = totalAllowances(detailB);
    if (aAllow !== bAllow)
      diff["allowances"] = {
        a: aAllow,
        b: bAllow,
        delta: bAllow - aAllow,
        percent:
          aAllow !== 0
            ? (((bAllow - aAllow) / aAllow) * 100).toFixed(1) + "%"
            : "-",
      };

    const aDed = totalDeductions(detailA);
    const bDed = totalDeductions(detailB);
    if (aDed !== bDed)
      diff["deductions"] = {
        a: aDed,
        b: bDed,
        delta: bDed - aDed,
        percent:
          aDed !== 0 ? (((bDed - aDed) / aDed) * 100).toFixed(1) + "%" : "-",
      };

    setCompare(diff);
  };

  const formatCurrency = (num) =>
    new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
    }).format(num || 0);

  return (
    <Box sx={{ p: 3, width: "100%" }}>
      <Typography variant="h5" gutterBottom>
        Compare {viewType === "payslips" ? "Payslips" : "CTCs"}
      </Typography>

      {/* Toggle */}
      <Box my={2}>
        <Button
          variant={viewType === "payslips" ? "contained" : "outlined"}
          onClick={() => setViewType("payslips")}
        >
          Payslips
        </Button>
        <Button
          sx={{ ml: 1 }}
          variant={viewType === "ctcs" ? "contained" : "outlined"}
          onClick={() => setViewType("ctcs")}
        >
          CTCs
        </Button>
      </Box>

      {/* Select & Summary Full-Width */}
      {[
        { pos: "A", val: selectA, set: setSelectA },
        { pos: "B", val: selectB, set: setSelectB },
      ].map((sel) => (
        <Box key={sel.pos} mb={3}>
          <FormControl fullWidth>
            <InputLabel>
              Select {viewType} {sel.pos}
            </InputLabel>
            <Select
              value={sel.val}
              label={`Select ${viewType}`}
              onChange={(e) => {
                sel.set(e.target.value);
                loadDetail(e.target.value, sel.pos);
              }}
            >
              {viewType === "payslips"
                ? list.map((p) => (
                    <MenuItem key={p.id} value={p.id}>
                      {p.month}/{p.year} — {formatCurrency(p.netPay)}
                    </MenuItem>
                  ))
                : list.map((ctc) => (
                    <MenuItem key={ctc.id} value={ctc.id}>
                      {new Date(ctc.effectiveFrom).toLocaleDateString("en-GB")}{" "}
                      — {formatCurrency(ctc.grossCTC)}
                    </MenuItem>
                  ))}
            </Select>
          </FormControl>

          {/* Summary */}
          <Card sx={{ mt: 2 }}>
            <CardContent>
              {sel.val && (sel.pos === "A" ? detailA : detailB) ? (
                <>
                  <Typography variant="h6" gutterBottom>
                    Summary {sel.pos}
                  </Typography>
                  <Typography>
                    Basic: {formatCurrency((sel.pos === "A" ? detailA : detailB).basic)}
                  </Typography>
                  <Typography>
                    Allowances:{" "}
                    {formatCurrency(totalAllowances(sel.pos === "A" ? detailA : detailB))}
                  </Typography>
                  <Typography>
                    Deductions:{" "}
                    {formatCurrency(totalDeductions(sel.pos === "A" ? detailA : detailB))}
                  </Typography>
                  {"grossCTC" in (sel.pos === "A" ? detailA : detailB) && (
                    <Typography>
                      GrossCTC:{" "}
                      {formatCurrency((sel.pos === "A" ? detailA : detailB).grossCTC)}
                    </Typography>
                  )}
                  {"netPay" in (sel.pos === "A" ? detailA : detailB) && (
                    <Typography>
                      NetPay: {formatCurrency((sel.pos === "A" ? detailA : detailB).netPay)}
                    </Typography>
                  )}
                </>
              ) : (
                <Typography color="text.secondary">
                  Select a {viewType}
                </Typography>
              )}
            </CardContent>
          </Card>
        </Box>
      ))}

      {/* Compare Button */}
      <Box textAlign="center" my={3}>
        <Button
          variant="contained"
          size="large"
          disabled={!detailA || !detailB}
          onClick={doCompare}
        >
          Compare
        </Button>
      </Box>

      {/* Differences summary */}
      {compare && (
        <Card sx={{ mt: 2 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Differences
            </Typography>

            {Object.keys(compare).length === 0 ? (
              <Typography color="text.secondary">
                No differences found — both selections are identical.
              </Typography>
            ) : (
              Object.keys(compare).map((f) => {
                const d = compare[f];
                const positive = d.delta >= 0;
                return (
                  <Box
                    key={f}
                    display="flex"
                    justifyContent="space-between"
                    my={1}
                    p={1}
                    sx={{
                      bgcolor: positive
                        ? "rgba(76,175,80,0.1)"
                        : "rgba(244,67,54,0.1)",
                      borderRadius: 1,
                    }}
                  >
                    <Typography
                      sx={{ fontWeight: "bold", textTransform: "capitalize" }}
                    >
                      {f}
                    </Typography>
                    <Typography>
                      {formatCurrency(d.a)} → {formatCurrency(d.b)}
                    </Typography>
                    <Typography
                      sx={{
                        color: positive ? "success.main" : "error.main",
                        fontWeight: "bold",
                      }}
                    >
                      {positive ? "▲" : "▼"} {formatCurrency(d.delta)} ({d.percent})
                    </Typography>
                  </Box>
                );
              })
            )}
          </CardContent>
        </Card>
      )}

      {/* Charts */}
      {compare && (
        <Box mt={3}>
          <Paper sx={{ mb: 3, p: 2, width: "100%", height: 300 }}>
            <Line
              data={{
                labels: ["A", "B"],
                datasets: [
                  {
                    label: "Basic",
                    data: [detailA.basic, detailB.basic],
                    borderColor: "blue",
                    tension: 0.3,
                  },
                ],
              }}
              options={{ responsive: true, maintainAspectRatio: false }}
            />
          </Paper>

          <Paper sx={{ mb: 3, p: 2, width: "100%", height: 300 }}>
            <Line
              data={{
                labels: ["A", "B"],
                datasets: [
                  {
                    label: "Allowances",
                    data: [totalAllowances(detailA), totalAllowances(detailB)],
                    borderColor: "green",
                    tension: 0.3,
                  },
                ],
              }}
              options={{ responsive: true, maintainAspectRatio: false }}
            />
          </Paper>

          <Paper sx={{ p: 2, width: "100%", height: 300 }}>
            <Line
              data={{
                labels: ["A", "B"],
                datasets: [
                  {
                    label: "Deductions",
                    data: [totalDeductions(detailA), totalDeductions(detailB)],
                    borderColor: "red",
                    tension: 0.3,
                  },
                ],
              }}
              options={{ responsive: true, maintainAspectRatio: false }}
            />
          </Paper>
        </Box>
      )}
    </Box>
  );
}

export default Compare;