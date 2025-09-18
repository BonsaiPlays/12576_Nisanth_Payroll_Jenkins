import DownloadIcon from "@mui/icons-material/Download";
import ReplayIcon from "@mui/icons-material/Replay";
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { useEffect, useState, useContext } from "react";
import api from "../api/axiosClient";
import { formatDate } from "../utils/date";
import { SnackbarContext } from "../context/SnackbarProvider"; 

function AuditLogs({ role }) {
  const showSnackbar = useContext(SnackbarContext);

  const today = new Date();
  const yyyy = today.getFullYear();
  const mm = String(today.getMonth() + 1).padStart(2, "0");
  const dd = String(today.getDate()).padStart(2, "0");
  const todayStr = `${yyyy}-${mm}-${dd}`;

  const tenDaysAgo = new Date();
  tenDaysAgo.setDate(today.getDate() - 10);
  const yyyyAgo = tenDaysAgo.getFullYear();
  const mmAgo = String(tenDaysAgo.getMonth() + 1).padStart(2, "0");
  const ddAgo = String(tenDaysAgo.getDate()).padStart(2, "0");
  const tenDaysAgoStr = `${yyyyAgo}-${mmAgo}-${ddAgo}`;

  const [logs, setLogs] = useState([]);
  const [from, setFrom] = useState(tenDaysAgoStr);
  const [to, setTo] = useState(todayStr);
  const [search, setSearch] = useState("");
  const [showModal, setShowModal] = useState(false);

  const loadLogs = async () => {
    try {
      const { data } = await api.get("/audit", { params: { from, to } });
      setLogs(data.items || data);
      showSnackbar("Audit logs loaded", "success");
    } catch (err) {
      console.error("Failed to load audit logs", err);
      setLogs([]);
      showSnackbar("Failed to load audit logs", "error");
    }
  };

  const handleExport = async () => {
    try {
      const res = await api.get("/audit/export", {
        params: { from, to },
        responseType: "blob",
      });

      const url = window.URL.createObjectURL(res.data);
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute(
        "download",
        `${role}Audit_${formatDate(from)}_${formatDate(to)}.xlsx`
      );
      document.body.appendChild(link);
      link.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(link);

      showSnackbar("Export successful!", "success");
    } catch (err) {
      console.error("Export failed:", err);
      showSnackbar("Export failed. Try again later.", "error");
    } finally {
      setShowModal(false);
    }
  };

  useEffect(() => {
    loadLogs();
  }, [from, to]);

  const filteredLogs = logs.filter((l) => {
    const text =
      `${l.entityType} ${l.action} ${l.details} ${l.performedBy}`.toLowerCase();
    return text.includes(search.toLowerCase());
  });

  const actionColor = (action) => {
    if (action === "Created") return "success";
    if (action === "Deleted") return "error";
    return "primary";
  };

  return (
    <Box
      sx={{ p: 3, display: "flex", flexDirection: "column", height: "100%" }}
    >
      <Typography variant="h5" gutterBottom>
        {role} Audit Logs
      </Typography>

      {/* Filters Row */}
      <Box display="flex" gap={2} mb={2}>
        <TextField
          type="date"
          label="From"
          InputLabelProps={{ shrink: true }}
          value={from}
          onChange={(e) => {
            let val = e.target.value;
            if (val > to) val = to;
            if (val > todayStr) val = todayStr;
            setFrom(val);
          }}
          sx={{ width: 180 }}
        />
        <TextField
          type="date"
          label="To"
          InputLabelProps={{ shrink: true }}
          value={to}
          onChange={(e) => {
            let val = e.target.value;
            if (val < from) val = from;
            if (val > todayStr) val = todayStr;
            setTo(val);
          }}
          sx={{ width: 180 }}
        />
        <TextField
          label="Search logs..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          fullWidth
        />
        <Tooltip title="Reload">
          <Button
            variant="outlined"
            startIcon={<ReplayIcon />}
            onClick={loadLogs}
          >
            Load
          </Button>
        </Tooltip>
        <Tooltip title="Export logs">
          <Button
            color="success"
            variant="contained"
            startIcon={<DownloadIcon />}
            onClick={() => setShowModal(true)}
          >
            Export
          </Button>
        </Tooltip>
      </Box>

      {/* Audit Table */}
      <TableContainer component={Paper} sx={{ flex: 1, overflow: "auto" }}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Entity</TableCell>
              <TableCell>Action</TableCell>
              <TableCell>Details</TableCell>
              <TableCell>By</TableCell>
              <TableCell>At</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredLogs.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  No records
                </TableCell>
              </TableRow>
            ) : (
              filteredLogs.map((l, idx) => (
                <TableRow key={idx} hover>
                  <TableCell>{l.entityType}</TableCell>
                  <TableCell>
                    <Chip
                      label={l.action}
                      color={actionColor(l.action)}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>{l.details}</TableCell>
                  <TableCell>{l.performedBy}</TableCell>
                  <TableCell>{formatDate(l.performedAt)}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Export Confirmation Modal */}
      <Dialog open={showModal} onClose={() => setShowModal(false)}>
        <DialogTitle>Confirm Export</DialogTitle>
        <DialogContent>
          <Typography>
            Do you want to export logs between <b>{from}</b> and <b>{to}</b> ?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowModal(false)}>Cancel</Button>
          <Button onClick={handleExport} variant="contained" color="success">
            Export
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default AuditLogs;