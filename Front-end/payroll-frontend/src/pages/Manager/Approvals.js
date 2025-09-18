/*
As an HR Manager, I want to review and approve CTC structures and pay slips submitted by HR so that payroll is validated before release. 
As an HR Manager I want to release the approved pay slip so that the employees can get their pay slip. 
As an HR Manager, I want to generate, and export pay slips in PDF format for compliance and audit purposes. 
*/

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

import FileDownloadIcon from "@mui/icons-material/FileDownload";
import VisibilityIcon from "@mui/icons-material/Visibility";
import api from "../../api/axiosClient";
import { formatDate } from "../../utils/date";
import { SnackbarContext } from "../../context/SnackbarProvider";

function Approvals() {
  const showSnackbar = useContext(SnackbarContext);
  const [records, setRecords] = useState([]);
  const [loading, setLoading] = useState(false);
  const [viewType, setViewType] = useState("payslips");
  const [modal, setModal] = useState({ show: false, entity: null });
  const [search, setSearch] = useState("");

  // Export modal
  const [exportModal, setExportModal] = useState(false);
  const [employees, setEmployees] = useState([]);
  const [selectedEmp, setSelectedEmp] = useState("");

  // Dynamic pagination
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [totalItems, setTotalItems] = useState(0);

  const [allRecords, setAllRecords] = useState([]);

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await api.get(`/hr-manager/${viewType}?pageSize=1000`);
      const allItems = data.items || [];
      setAllRecords(allItems);

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
                  ).includes(search)
                : formatDate(item.effectiveFrom)?.includes(search))
          )
        : allItems;

      // Calculate pagination
      const startIndex = page * pageSize;
      const endIndex = startIndex + pageSize;

      setRecords(filteredItems.slice(startIndex, endIndex));
      setTotalItems(filteredItems.length);
    } catch {
      showSnackbar(`Error loading ${viewType}`, "error");
    }
    setLoading(false);
  };

  useEffect(() => {
    load();
  }, [viewType, page, pageSize, search]);

  useEffect(() => {
    setAllRecords([]); // Clear all records when viewType changes
    setPage(0); // Reset to first page
    setSearch(""); // Clear search
  }, [viewType]);

  useEffect(() => {
    load();
  }, [page, pageSize, search, viewType]);

  const changeStatus = async (entity, status) => {
    try {
      if (entity.type === "Payslip") {
        await api.post(
          `/hr-manager/payslips/${entity.id}/status?status=${status}`
        );
      } else {
        await api.post(`/hr-manager/ctcs/${entity.id}/status?status=${status}`);
      }
      showSnackbar(`${entity.type} marked as ${status}`, "success");
      load();
    } catch (error) {
      if (error.response?.status === 409) {
        showSnackbar("Another Approved CTC exists for this year", "error");
        return;
      }
      showSnackbar(`Failed to set ${entity.type} status`, "error");
    } finally {
      setModal({ show: false, entity: null });
    }
  };

  const releasePayslip = async (entity) => {
    try {
      await api.post(`/hr-manager/payslips/${entity.id}/release`);
      showSnackbar("Payslip released to employee", "success");
      load();
    } catch {
      showSnackbar("Failed to release payslip", "error");
    }
  };

  const openReview = async (entity) => {
    try {
      const { data } = await api.get(
        viewType === "payslips"
          ? `/hr-manager/payslips/${entity.id}/detail`
          : `/hr-manager/ctcs/${entity.id}/detail`
      );

      let normalized = {
        ...data,
        type: viewType === "payslips" ? "Payslip" : "CTC",
      };

      // Do not override status if backend provides it!
      // Just handle released separately
      if (viewType === "payslips" && normalized.isReleased) {
        normalized.status = 3; // special value for Released
      }

      setModal({ show: true, entity: normalized });
    } catch {
      showSnackbar("Could not load details", "error");
    }
  };

  const downloadPdf = async (entity) => {
    try {
      const url =
        viewType === "payslips"
          ? `/hr-manager/exports/payslips/pdf/${entity.id}`
          : `/hr-manager/ctcs/${entity.id}/pdf`;
      const res = await api.get(url, { responseType: "blob" });
      const file = new Blob([res.data], { type: "application/pdf" });
      const link = document.createElement("a");
      link.href = URL.createObjectURL(file);
      // Normalize employee name (remove spaces)
      const empName =
        entity.employee?.fullName?.replace(/\s+/g, "") || "Employee";

      if (viewType === "payslips") {
        const month = String(entity.month).padStart(2, "0");
        const year = entity.year;
        link.download = `${empName}_PaySlip_${month}-${year}.pdf`;
      } else {
        // use dd-MM-yyyy for CTC effectiveFrom
        const effective = formatDate(entity.effectiveFrom);
        link.download = `${empName}_CTC_${effective}.pdf`;
      }
      link.click();
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
          ? `/hr-manager/exports/payslips/excel?employeeId=${selectedEmp}`
          : `/hr-manager/exports/ctcs/excel?employeeId=${selectedEmp}`;

      const res = await api.get(endpoint, { responseType: "blob" });
      const file = new Blob([res.data], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      });
      const link = document.createElement("a");
      link.href = URL.createObjectURL(file);
      link.download = `${selectedEmp}_${viewType}.xlsx`;
      link.click();
      setExportModal(false);
      setSelectedEmp("");
    } catch {
      showSnackbar("Export failed", "error");
    }
  };

  const statusBadge = (r) => {
    if (r.isReleased)
      return <Chip label="Released" color="info" size="small" />;
    if (r.status === 0)
      return <Chip label="Pending" color="warning" size="small" />;
    if (r.status === 1)
      return <Chip label="Approved" color="success" size="small" />;
    if (r.status === 2)
      return <Chip label="Rejected" color="error" size="small" />;
    return <Chip label="Unknown" />;
  };

  return (
    <Box
      sx={{ p: 3, display: "flex", flexDirection: "column", height: "100%" }}
      data-testid="approvals-page"
    >
      <Typography variant="h5" gutterBottom data-testid="approvals-title">
        HR Manager Approvals
      </Typography>

      {/* Toggle + Search + Export */}
      <Box
        display="flex"
        justifyContent="space-between"
        mb={2}
        data-testid="approvals-controls"
      >
        <Box display="flex" gap={1} data-testid="view-type-toggle">
          <Button
            variant={viewType === "payslips" ? "contained" : "outlined"}
            onClick={() => {
              setViewType("payslips");
              setAllRecords([]);
              setPage(0);
              setSearch("");
            }}
            data-testid="payslips-view-button"
          >
            Payslips
          </Button>
          <Button
            variant={viewType === "ctcs" ? "contained" : "outlined"}
            onClick={() => {
              setViewType("ctcs");
              setAllRecords([]);
              setPage(0);
              setSearch("");
            }}
            data-testid="ctcs-view-button"
          >
            CTCs
          </Button>
        </Box>
        <Box display="flex" gap={1} data-testid="search-export-controls">
          <TextField
            size="small"
            placeholder="Search..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(0);
            }}
            inputProps={{ "data-testid": "search-input" }}
          />
          <Button
            variant="outlined"
            color="secondary"
            onClick={() => {
              setSearch("");
              setPage(0);
            }}
            data-testid="reset-search-button"
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
            data-testid="export-excel-button"
          >
            Export Excel
          </Button>
        </Box>
      </Box>

      <TableContainer
        component={Paper}
        sx={{ flex: 1 }}
        data-testid="approvals-table-container"
      >
        <Table stickyHeader data-testid="approvals-table">
          <TableHead>
            {viewType === "payslips" ? (
              <TableRow data-testid="payslips-header">
                <TableCell data-testid="month-header">Month</TableCell>
                <TableCell data-testid="year-header">Year</TableCell>
                <TableCell data-testid="netpay-header">NetPay</TableCell>
                <TableCell data-testid="status-header">Status</TableCell>
                <TableCell data-testid="employee-header">Employee</TableCell>
                <TableCell data-testid="actions-header">Actions</TableCell>
              </TableRow>
            ) : (
              <TableRow data-testid="ctcs-header">
                <TableCell data-testid="effective-from-header">
                  Effective From
                </TableCell>
                <TableCell data-testid="gross-ctc-header">Gross CTC</TableCell>
                <TableCell data-testid="status-header">Status</TableCell>
                <TableCell data-testid="employee-header">Employee</TableCell>
                <TableCell data-testid="actions-header">Actions</TableCell>
              </TableRow>
            )}
          </TableHead>
          <TableBody data-testid="approvals-table-body">
            {loading ? (
              <TableRow data-testid="loading-row">
                <TableCell
                  colSpan={6}
                  align="center"
                  data-testid="loading-cell"
                >
                  <CircularProgress size={24} data-testid="loading-spinner" />
                </TableCell>
              </TableRow>
            ) : records.length === 0 ? (
              <TableRow data-testid="no-records-row">
                <TableCell
                  colSpan={6}
                  align="center"
                  data-testid="no-records-cell"
                >
                  No {viewType} found
                </TableCell>
              </TableRow>
            ) : (
              records.map((r, index) => (
                <TableRow
                  key={`${r.type}-${r.id}`}
                  hover
                  data-testid={`record-row-${index}`}
                >
                  {viewType === "payslips" ? (
                    <>
                      <TableCell data-testid={`month-cell-${index}`}>
                        {r.month}
                      </TableCell>
                      <TableCell data-testid={`year-cell-${index}`}>
                        {r.year}
                      </TableCell>
                      <TableCell data-testid={`netpay-cell-${index}`}>
                        ₹{r.netPay}
                      </TableCell>
                    </>
                  ) : (
                    <>
                      <TableCell data-testid={`effective-from-cell-${index}`}>
                        {formatDate(r.effectiveFrom)}
                      </TableCell>
                      <TableCell data-testid={`gross-ctc-cell-${index}`}>
                        ₹{r.grossCTC}
                      </TableCell>
                    </>
                  )}
                  <TableCell data-testid={`status-cell-${index}`}>
                    {statusBadge(r)}
                  </TableCell>
                  <TableCell data-testid={`employee-cell-${index}`}>
                    {r.employee?.fullName
                      ? `${r.employee.fullName} (${r.employee.email})`
                      : "(Unknown)"}
                  </TableCell>
                  <TableCell data-testid={`actions-cell-${index}`}>
                    <Tooltip title="Review">
                      <IconButton
                        onClick={() => openReview(r)}
                        data-testid={`review-button-${index}`}
                      >
                        <VisibilityIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Download PDF">
                      <IconButton
                        onClick={() => downloadPdf(r)}
                        data-testid={`download-pdf-button-${index}`}
                      >
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
        sx={{ mt: 1 }}
        nextIconButtonProps={{
          disabled:
            records.length < pageSize || (page + 1) * pageSize >= totalItems,
        }}
        backIconButtonProps={{
          disabled: page === 0,
        }}
        labelDisplayedRows={({ from, to }) => `${from}–${to}`}
        data-testid="pagination-controls"
      />

      {/* Review Modal */}
      <Dialog
        open={modal.show}
        onClose={() => setModal({ show: false, entity: null })}
        maxWidth="md"
        fullWidth
        data-testid="review-modal"
      >
        <DialogTitle data-testid="review-modal-title">
          Review & Take Action
        </DialogTitle>
        <DialogContent dividers data-testid="review-modal-content">
          {modal.entity && (
            <>
              <Typography data-testid="employee-info">
                <b>Employee:</b> {modal.entity.employee?.fullName} (
                {modal.entity.employee?.email})
              </Typography>
              {modal.entity.type === "Payslip" && (
                <>
                  <Typography data-testid="month-year-info">
                    <b>Month/Year:</b> {modal.entity.month}/{modal.entity.year}
                  </Typography>
                  <Typography data-testid="netpay-info">
                    <b>NetPay:</b> ₹{modal.entity.netPay}
                  </Typography>
                  <Typography data-testid="tax-info">
                    <b>Tax:</b> ₹{modal.entity.taxDeducted}
                  </Typography>
                </>
              )}
              {modal.entity.type === "CTC" && (
                <>
                  <Typography data-testid="effective-from-info">
                    <b>Effective From:</b>{" "}
                    {formatDate(modal.entity.effectiveFrom)}
                  </Typography>
                  <Typography data-testid="basic-info">
                    <b>Basic:</b> ₹{modal.entity.basic}
                  </Typography>
                  <Typography data-testid="hra-info">
                    <b>HRA:</b> ₹{modal.entity.hra}
                  </Typography>
                  <Typography data-testid="gross-ctc-info">
                    <b>Gross CTC:</b> ₹{modal.entity.grossCTC}
                  </Typography>
                  <Typography data-testid="tax-percent-info">
                    <b>Tax %:</b> {modal.entity.taxPercent}%
                  </Typography>
                </>
              )}
            </>
          )}
        </DialogContent>
        <DialogActions data-testid="review-modal-actions">
          <Button
            onClick={() => setModal({ show: false, entity: null })}
            data-testid="close-modal-button"
          >
            Close
          </Button>
          {modal.entity && modal.entity.type === "Payslip" && (
            <>
              {(() => {
                const s = modal.entity.status;
                const released = modal.entity.isReleased;

                if (released) return null;

                if (s === 0) {
                  // Pending
                  return (
                    <>
                      <Button
                        color="success"
                        onClick={() => changeStatus(modal.entity, "Approved")}
                        data-testid="approve-payslip-button"
                      >
                        Approve
                      </Button>
                      <Button
                        color="error"
                        onClick={() => changeStatus(modal.entity, "Rejected")}
                        data-testid="reject-payslip-button"
                      >
                        Reject
                      </Button>
                    </>
                  );
                }

                if (s === 1) {
                  // Approved
                  return (
                    <>
                      <Button
                        color="primary"
                        onClick={() => releasePayslip(modal.entity)}
                        data-testid="release-payslip-button"
                      >
                        Release
                      </Button>
                      <Button
                        color="error"
                        onClick={() => changeStatus(modal.entity, "Rejected")}
                        data-testid="reject-approved-payslip-button"
                      >
                        Reject
                      </Button>
                    </>
                  );
                }

                if (s === 2) {
                  // Rejected
                  return (
                    <Button
                      color="success"
                      onClick={() => changeStatus(modal.entity, "Approved")}
                      data-testid="approve-rejected-payslip-button"
                    >
                      Approve
                    </Button>
                  );
                }
              })()}
            </>
          )}
          {modal.entity && modal.entity.type === "CTC" && (
            <>
              {(() => {
                const s = modal.entity.status;

                if (s === 0) {
                  // Pending
                  return (
                    <>
                      <Button
                        color="success"
                        onClick={() => changeStatus(modal.entity, "Approved")}
                        data-testid="approve-ctc-button"
                      >
                        Approve
                      </Button>
                      <Button
                        color="error"
                        onClick={() => changeStatus(modal.entity, "Rejected")}
                        data-testid="reject-ctc-button"
                      >
                        Reject
                      </Button>
                    </>
                  );
                }

                if (s === 1) {
                  // Approved
                  return (
                    <>
                      <Button
                        color="error"
                        onClick={() => changeStatus(modal.entity, "Rejected")}
                        data-testid="reject-approved-ctc-button"
                      >
                        Reject
                      </Button>
                    </>
                  );
                }

                if (s === 2) {
                  // Rejected
                  return (
                    <Button
                      color="success"
                      onClick={() => changeStatus(modal.entity, "Approved")}
                      data-testid="approve-rejected-ctc-button"
                    >
                      Approve
                    </Button>
                  );
                }
              })()}
            </>
          )}
        </DialogActions>
      </Dialog>

      {/* Export Modal */}
      <Dialog
        open={exportModal}
        onClose={() => setExportModal(false)}
        maxWidth="xs"
        fullWidth
        data-testid="export-modal"
      >
        <DialogTitle data-testid="export-modal-title">
          Export as Excel
        </DialogTitle>
        <DialogContent dividers data-testid="export-modal-content">
          <TextField
            select
            fullWidth
            label="Select Employee"
            value={selectedEmp}
            onChange={(e) => setSelectedEmp(e.target.value)}
            inputProps={{ "data-testid": "employee-select" }}
          >
            <MenuItem value="" data-testid="default-employee-option">
              -- Choose Employee --
            </MenuItem>
            {employees.map((emp) => (
              <MenuItem
                key={emp.id}
                value={emp.id}
                data-testid={`employee-option-${emp.id}`}
              >
                {emp.fullName} ({emp.email})
              </MenuItem>
            ))}
          </TextField>
        </DialogContent>
        <DialogActions data-testid="export-modal-actions">
          <Button
            onClick={() => setExportModal(false)}
            data-testid="cancel-export-button"
          >
            Cancel
          </Button>
          <Button
            variant="contained"
            color="success"
            onClick={exportExcel}
            data-testid="confirm-export-button"
          >
            Export
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default Approvals;
