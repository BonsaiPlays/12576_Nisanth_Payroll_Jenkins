import {
  Box,
  Button,
  CircularProgress,
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
  Tooltip,
  Typography,
} from "@mui/material";
import React, { useContext, useEffect, useState } from "react";

import { AuthContext } from "../../context/AuthContext";
import MonetizationOnIcon from "@mui/icons-material/MonetizationOn";
import ReceiptIcon from "@mui/icons-material/Receipt";
import api from "../../api/axiosClient";
import { SnackbarContext } from "../../context/SnackbarProvider";
import { useNavigate } from "react-router-dom";

function Employees() {
  const { user } = useContext(AuthContext);
  const [employees, setEmployees] = useState([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [totalItems, setTotalItems] = useState(0);
  const [allEmployees, setAllEmployees] = useState([]);

  const showSnackbar = useContext(SnackbarContext);
  const navigate = useNavigate();

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await api.get(`/hr/employees?pageSize=1000`);
      const allItems = data.items || [];
      setAllEmployees(allItems);

      // Filter based on search
      const filteredItems = search
        ? allItems.filter(
            (emp) =>
              emp.fullName?.toLowerCase().includes(search.toLowerCase()) ||
              emp.email?.toLowerCase().includes(search.toLowerCase()) ||
              emp.department?.toLowerCase().includes(search.toLowerCase()) ||
              emp.id?.toString().includes(search)
          )
        : allItems;

      // Calculate pagination
      const startIndex = page * pageSize;
      const endIndex = startIndex + pageSize;

      setEmployees(filteredItems.slice(startIndex, endIndex));
      setTotalItems(filteredItems.length);
    } catch {
      showSnackbar("Failed loading employees", "error");
    }
    setLoading(false);
  };

  useEffect(() => {
    load();
  }, [page, pageSize, search]);

  const basePath = "/hr";

  return (
    <Box
      sx={{ p: 3, height: "100%", display: "flex", flexDirection: "column" }}
    >
      <Box
        display="flex"
        justifyContent="space-between"
        alignItems="center"
        mb={2}
      >
        <Typography variant="h5">Employees</Typography>

        <Box display="flex" gap={1}>
          <TextField
            size="small"
            label="Search by name/email/department"
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
        </Box>
      </Box>

      <TableContainer component={Paper} sx={{ flex: 1 }}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>ID</TableCell>
              <TableCell>Name</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Department</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            ) : employees.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} align="center">
                  No employees found.
                </TableCell>
              </TableRow>
            ) : (
              employees.map((emp) => (
                <TableRow key={emp.id} hover>
                  <TableCell>{emp.id}</TableCell>
                  <TableCell>{emp.fullName}</TableCell>
                  <TableCell>{emp.email}</TableCell>
                  <TableCell>{emp.department || "-"}</TableCell>
                  <TableCell align="center">
                    <Tooltip title="Assign/View CTC">
                      <IconButton
                        onClick={() =>
                          navigate(`${basePath}/ctc?empId=${emp.id}`)
                        }
                        color="primary"
                        aria-label="assign or view CTC"
                      >
                        <MonetizationOnIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Generate Payslip">
                      <IconButton
                        onClick={() =>
                          navigate(`${basePath}/payslip?empId=${emp.id}`)
                        }
                        color="secondary"
                        aria-label="generate payslip"
                      >
                        <ReceiptIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

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
      />
    </Box>
  );
}

export default Employees;
