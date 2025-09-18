import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import React, { useEffect, useState, useContext } from "react";

import DownloadIcon from "@mui/icons-material/Download";
import VisibilityIcon from "@mui/icons-material/Visibility";
import api from "../../api/axiosClient";
import { SnackbarContext } from "../../context/SnackbarProvider";

function PayslipList() {
  const [payslips, setPayslips] = useState([]);
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState(null);
  const [showModal, setShowModal] = useState(false);

  const [allPayslips, setAllPayslips] = useState([]);

  // Pagination
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [totalItems, setTotalItems] = useState(0);
  const [loading, setLoading] = useState(false);

  const showSnackbar = useContext(SnackbarContext);

  const monthNames = [
    "",
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
  ];

  const formatCurrency = (num) =>
    new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
    }).format(num);

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await api.get(`/employee/payslips?pageSize=1000`);
      const allItems = data.items || [];
      setAllPayslips(allItems);

      // Filter based on search
      const filteredItems = search
        ? allItems.filter(
            (item) =>
              item.year?.toString().includes(search.toLowerCase()) ||
              monthNames[item.month]
                ?.toLowerCase()
                .includes(search.toLowerCase()) ||
              item.netPay?.toString().includes(search)
          )
        : allItems;

      // Calculate pagination
      const startIndex = page * pageSize;
      const endIndex = startIndex + pageSize;

      setPayslips(filteredItems.slice(startIndex, endIndex));
      setTotalItems(filteredItems.length);
    } catch {
      showSnackbar("Failed to load payslips", "error");
    }
    setLoading(false);
  };

  useEffect(() => {
    load();
  }, [page, pageSize, search]);

  const downloadPdf = async (id, year, month) => {
    try {
      const res = await api.get(`/employee/payslips/${id}/pdf`, {
        responseType: "blob",
      });
      const url = window.URL.createObjectURL(
        new Blob([res.data], { type: "application/pdf" })
      );
      const link = document.createElement("a");
      link.href = url;
      link.download = `payslip_${year}_${month}.pdf`;
      link.click();
      link.remove();
      showSnackbar("PDF downloaded successfully", "success");
    } catch {
      showSnackbar("Unable to download PDF", "error");
    }
  };

  const loadDetail = async (id) => {
    try {
      const { data } = await api.get(`/employee/payslips/${id}`);
      setSelected(data);
      setShowModal(true);
    } catch {
      showSnackbar("Unable to load payslip detail", "error");
    }
  };

  return (
    <Box
      sx={{ p: 3, height: "100%", display: "flex", flexDirection: "column" }}
    >
      <Typography variant="h5" gutterBottom>
        My Payslips
      </Typography>

      {/* Search bar */}
      <Box display="flex" gap={1} mb={2}>
        <TextField
          placeholder="Search by year, month, or net pay..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(0);
          }}
          size="small"
          fullWidth
        />
        <Button
          variant="outlined"
          color="secondary"
          onClick={() => {
            setSearch("");
            setPage(0);
          }}
        >
          Reset
        </Button>
      </Box>

      {/* Table */}
      <TableContainer component={Paper} sx={{ flex: 1 }}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Year</TableCell>
              <TableCell>Month</TableCell>
              <TableCell>Net Pay</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            ) : payslips.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  No payslips found
                </TableCell>
              </TableRow>
            ) : (
              payslips.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>{p.year}</TableCell>
                  <TableCell>{monthNames[p.month]}</TableCell>
                  <TableCell>{formatCurrency(p.netPay)}</TableCell>
                  <TableCell>
                    {p.isReleased ? (
                      <Chip label="Released" color="success" size="small" />
                    ) : p.isApproved ? (
                      <Chip label="Approved" color="warning" size="small" />
                    ) : (
                      <Chip label="Pending" color="default" size="small" />
                    )}
                  </TableCell>
                  <TableCell>
                    {p.isReleased && (
                      <>
                        <IconButton
                          color="primary"
                          onClick={() => loadDetail(p.id)}
                        >
                          <VisibilityIcon />
                        </IconButton>
                        <IconButton
                          color="secondary"
                          onClick={() => downloadPdf(p.id, p.year, p.month)}
                        >
                          <DownloadIcon />
                        </IconButton>
                      </>
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Pagination */}
      <TablePagination
        component="div"
        count={totalItems}
        page={page}
        onPageChange={(e, newPage) => setPage(newPage)}
        rowsPerPage={pageSize}
        onRowsPerPageChange={(e) => {
          setPageSize(parseInt(e.target.value, 10));
          setPage(0);
        }}
        rowsPerPageOptions={[5, 10, 25, 50]}
        labelDisplayedRows={({ from, to }) => `${from}–${to}`}
        nextIconButtonProps={{
          disabled:
            payslips.length < pageSize || (page + 1) * pageSize >= totalItems,
        }}
        backIconButtonProps={{
          disabled: page === 0,
        }}
      />

      {/* Payslip Detail Modal */}
      <Dialog
        open={showModal}
        onClose={() => setShowModal(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Payslip Detail</DialogTitle>
        <DialogContent dividers>
          {selected && (
            <>
              <Typography>
                <b>Year/Month:</b> {selected.year} /{" "}
                {monthNames[selected.month]}
              </Typography>
              <Typography>
                <b>Basic:</b> {formatCurrency(selected.basic)}
              </Typography>
              <Typography>
                <b>HRA:</b> {formatCurrency(selected.hra)}
              </Typography>
              <Typography mt={2} variant="subtitle1">
                Allowances
              </Typography>
              {selected.allowanceItems?.length > 0 ? (
                <ul>
                  {selected.allowanceItems.map((a, i) => (
                    <li key={i}>
                      {a.label}: {formatCurrency(a.amount)}
                    </li>
                  ))}
                </ul>
              ) : (
                <Typography color="text.secondary">None</Typography>
              )}
              <Typography mt={2} variant="subtitle1">
                Deductions
              </Typography>
              {selected.deductionItems?.length > 0 ? (
                <ul>
                  {selected.deductionItems.map((d, i) => (
                    <li key={i}>
                      {d.label}: {formatCurrency(d.amount)}
                    </li>
                  ))}
                </ul>
              ) : (
                <Typography color="text.secondary">None</Typography>
              )}
              <Typography>
                <b>Tax Deducted:</b> {formatCurrency(selected.taxDeducted)}
              </Typography>
              <Typography>
                <b>Net Pay:</b>{" "}
                <span style={{ color: "green" }}>
                  {formatCurrency(selected.netPay)}
                </span>
              </Typography>
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowModal(false)}>Close</Button>
          {selected && (
            <Button
              variant="contained"
              onClick={() =>
                downloadPdf(selected.id, selected.year, selected.month)
              }
              startIcon={<DownloadIcon />}
            >
              Download PDF
            </Button>
          )}
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default PayslipList;
