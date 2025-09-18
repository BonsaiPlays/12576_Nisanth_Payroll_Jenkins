/*
As an HR, I want to generate, and export pay slips in PDF and Excel format so that they can be used for compliance and audit purposes. 
*/

import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import React, { useEffect, useState, useContext } from "react";
import { SnackbarContext } from "../../context/SnackbarProvider";

import FileDownloadIcon from "@mui/icons-material/FileDownload";
import VisibilityIcon from "@mui/icons-material/Visibility";
import api from "../../api/axiosClient";
import { formatDate } from "../../utils/date";

function Exports() {
  const [records, setRecords] = useState([]);
  const [loading, setLoading] = useState(false);
  const [viewType, setViewType] = useState("payslips");
  const [search, setSearch] = useState("");

  const [allRecords, setAllRecords] = useState([]);

  // pagination
  const [page, setPage] = useState(0); // 0-based
  const [pageSize, setPageSize] = useState(10);
  const [totalItems, setTotalItems] = useState(0);

  const [exportModal, setExportModal] = useState(false);
  const [employees, setEmployees] = useState([]);
  const [selectedEmp, setSelectedEmp] = useState("");

  const [modal, setModal] = useState({ show: false, entity: null });

  const showSnackbar = useContext(SnackbarContext);

  const currency = new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 2,
  });

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await api.get(`/hr-manager/${viewType}?pageSize=1000`);
      const allItems = data.items || [];

      // Filter based on search
      const filteredItems = search
        ? allItems.filter(
            (item) =>
              item.employee?.fullName
                ?.toLowerCase()
                .includes(search.toLowerCase()) ||
              item.employee?.email
                ?.toLowerCase()
                .includes(search.toLowerCase()) ||
              (viewType === "payslips"
                ? (
                    item.month?.toString() +
                    "/" +
                    item.year?.toString()
                  ).includes(search) || item.netPay?.toString().includes(search)
                : formatDate(item.effectiveFrom)?.includes(search) ||
                  item.grossCTC?.toString().includes(search))
          )
        : allItems;

      // Calculate pagination
      const startIndex = page * pageSize;
      const endIndex = startIndex + pageSize;

      setAllRecords(allItems); // Store all records
      setRecords(filteredItems.slice(startIndex, endIndex)); // Set paginated records
      setTotalItems(filteredItems.length);
    } catch (error) {
      console.error("Load error:", error);
      showSnackbar(`Error loading ${viewType}`, "error");
    }
    setLoading(false);
  };

  useEffect(() => {
    load();
  }, [viewType]); // Only reload when viewType changes

  useEffect(() => {
    // Handle pagination and filtering from existing data
    const filteredItems = search
      ? allRecords.filter(
          (item) =>
            item.employee?.fullName
              ?.toLowerCase()
              .includes(search.toLowerCase()) ||
            item.employee?.email
              ?.toLowerCase()
              .includes(search.toLowerCase()) ||
            (viewType === "payslips"
              ? (item.month?.toString() + "/" + item.year?.toString()).includes(
                  search
                ) || item.netPay?.toString().includes(search)
              : formatDate(item.effectiveFrom)?.includes(search) ||
                item.grossCTC?.toString().includes(search))
        )
      : allRecords;

    const startIndex = page * pageSize;
    const endIndex = startIndex + pageSize;

    setRecords(filteredItems.slice(startIndex, endIndex));
    setTotalItems(filteredItems.length);
  }, [page, pageSize, search, allRecords]);

  //Debug
  useEffect(() => {
    console.log("Records:", records);
    console.log("Total Items:", totalItems);
    console.log("Current Page:", page);
    console.log("Page Size:", pageSize);
  }, [records, totalItems, page, pageSize]);

  const openReview = async (entity) => {
    try {
      let { data } = await api.get(
        viewType === "payslips"
          ? `/hr/payslips/${entity.id}/detail`
          : `/hr/ctcs/${entity.id}/detail`
      );
      setModal({ show: true, entity: data });
    } catch {
      showSnackbar("Could not load details", "error");
    }
  };

  const downloadPdf = async (entity) => {
    try {
      const url =
        viewType === "payslips"
          ? `/hr/exports/payslips/pdf/${entity.id}`
          : `/hr/ctcs/${entity.id}/pdf`;
      const res = await api.get(url, { responseType: "blob" });
      const file = new Blob([res.data], { type: "application/pdf" });
      const link = document.createElement("a");
      link.href = URL.createObjectURL(file);
      const empName =
        entity.employee?.fullName?.replace(/\s+/g, "") || "Employee";

      if (viewType === "payslips") {
        // Format month/year as always 2-digit month + 4-digit year
        const month = String(entity.month).padStart(2, "0");
        const year = entity.year;
        link.download = `${empName}_PaySlip_${month}-${year}.pdf`;
      } else {
        // For CTCs, use EffectiveFrom date formatted
        const effDate = formatDate(entity.effectiveFrom); // already dd-MM-yyyy
        link.download = `${empName}_CTC_${effDate}.pdf`;
      }
      link.click();
      showSnackbar("PDF downloaded", "success");
    } catch {
      showSnackbar("Failed to download PDF", "error");
    }
  };

  const loadEmployees = async () => {
    try {
      const { data } = await api.get("/hr/employees?page=1&pageSize=200");
      setEmployees(data.items || []);
    } catch {
      showSnackbar("Failed to load employees list", "error");
    }
  };

  const exportExcel = async () => {
    if (!selectedEmp) {
      showSnackbar("Select an employee first", "error");
      return;
    }
    try {
      const endpoint =
        viewType === "payslips"
          ? `/hr/exports/payslips/excel?employeeId=${selectedEmp}`
          : `/hr/exports/ctcs/excel?employeeId=${selectedEmp}`;
      const res = await api.get(endpoint, { responseType: "blob" });
      const file = new Blob([res.data], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      });
      const link = document.createElement("a");
      link.href = URL.createObjectURL(file);
      link.download = `${selectedEmp}_${viewType}.xlsx`;
      link.click();
      showSnackbar("Excel exported!", "success");
      setExportModal(false);
      setSelectedEmp("");
    } catch {
      showSnackbar("Export failed", "error");
    }
  };

  return (
    <Box
      sx={{ p: 3, display: "flex", flexDirection: "column", height: "100%" }}
    >
      <Typography variant="h5" gutterBottom>
        HR Data Exports
      </Typography>

      {/* Controls */}
      <Box display="flex" justifyContent="space-between" mb={2}>
        <Box display="flex" gap={1}>
          <TextField
            size="small"
            placeholder="Search..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(0);
            }}
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
          <Button
            variant="contained"
            color="success"
            onClick={() => {
              loadEmployees();
              setExportModal(true);
            }}
          >
            Export Excel
          </Button>
        </Box>
      </Box>

      {/* Table */}
      <TableContainer component={Paper} sx={{ flex: 1, overflow: "auto" }}>
        <Table stickyHeader>
          <TableHead>
            {viewType === "payslips" ? (
              <TableRow>
                <TableCell>Month</TableCell>
                <TableCell>Year</TableCell>
                <TableCell>Net Pay</TableCell>
                <TableCell>Employee</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            ) : (
              <TableRow>
                <TableCell>Effective From</TableCell>
                <TableCell>Gross CTC</TableCell>
                <TableCell>Employee</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            )}
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            ) : records.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  No {viewType} found
                </TableCell>
              </TableRow>
            ) : (
              records.map((r) => (
                <TableRow key={r.id} hover>
                  {viewType === "payslips" ? (
                    <>
                      <TableCell>{r.month}</TableCell>
                      <TableCell>{r.year}</TableCell>
                      <TableCell>{currency.format(r.netPay)}</TableCell>
                    </>
                  ) : (
                    <>
                      <TableCell>{formatDate(r.effectiveFrom)}</TableCell>
                      <TableCell>{currency.format(r.grossCTC)}</TableCell>
                    </>
                  )}
                  <TableCell>
                    {r.employee?.fullName
                      ? `${r.employee.fullName} (${r.employee.email})`
                      : "(Unknown)"}
                  </TableCell>
                  <TableCell>
                    <Tooltip title="Review">
                      <IconButton onClick={() => openReview(r)}>
                        <VisibilityIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Download PDF">
                      <IconButton onClick={() => downloadPdf(r)}>
                        <FileDownloadIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Dynamic Pagination */}
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
        sx={{ mt: 1 }}
        labelDisplayedRows={({ from, to }) => `${from}–${to}`}
        nextIconButtonProps={{
          disabled:
            records.length < pageSize || (page + 1) * pageSize >= totalItems,
        }}
        backIconButtonProps={{
          disabled: page === 0,
        }}
      />

      {/* Review Modal */}
      <Dialog
        open={modal.show}
        onClose={() => setModal({ show: false, entity: null })}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Review Record</DialogTitle>
        <DialogContent dividers>
          {modal.entity && (
            <>
              <Typography>
                <b>Employee:</b> {modal.entity.employee?.fullName} (
                {modal.entity.employee?.email})
              </Typography>
              {modal.entity.type === "Payslip" && (
                <>
                  <Typography>
                    <b>Month/Year:</b> {modal.entity.month}/{modal.entity.year}
                  </Typography>
                  <Typography>
                    <b>NetPay:</b> {currency.format(modal.entity.netPay)}
                  </Typography>
                  <Typography>
                    <b>Tax:</b> {currency.format(modal.entity.taxDeducted)}
                  </Typography>
                </>
              )}
              {modal.entity.type === "CTC" && (
                <>
                  <Typography>
                    <b>Effective From:</b>{" "}
                    {formatDate(modal.entity.effectiveFrom)}
                  </Typography>
                  <Typography>
                    <b>Gross CTC:</b> {currency.format(modal.entity.grossCTC)}
                  </Typography>
                </>
              )}
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModal({ show: false, entity: null })}>
            Close
          </Button>
        </DialogActions>
      </Dialog>

      {/* Export Modal */}
      <Dialog
        open={exportModal}
        onClose={() => setExportModal(false)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Export as Excel</DialogTitle>
        <DialogContent dividers>
          <TextField
            select
            fullWidth
            label="Select Employee"
            value={selectedEmp}
            onChange={(e) => setSelectedEmp(e.target.value)}
          >
            <MenuItem value="">-- Choose Employee --</MenuItem>
            {employees.map((emp) => (
              <MenuItem key={emp.id} value={emp.id}>
                {emp.fullName} ({emp.email})
              </MenuItem>
            ))}
          </TextField>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setExportModal(false)}>Cancel</Button>
          <Button variant="contained" color="success" onClick={exportExcel}>
            Export
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default Exports;
