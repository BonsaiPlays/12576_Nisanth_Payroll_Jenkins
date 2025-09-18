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
  Select,
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

import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import RestartAltIcon from "@mui/icons-material/RestartAlt";
import api from "../../api/axiosClient";
import { SnackbarContext } from "../../context/SnackbarProvider";

function UserList() {
  const [users, setUsers] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);

  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [totalItems, setTotalItems] = useState(0);

  const [showCreate, setShowCreate] = useState(false);
  const [confirmAction, setConfirmAction] = useState(null);
  const [selectedUser, setSelectedUser] = useState(null);

  const [newUser, setNewUser] = useState({
    email: "",
    fullName: "",
    role: "",
    departmentId: "",
  });

  const [validationErrors, setValidationErrors] = useState({
    email: "",
    fullName: "",
  });

  const showSnackbar = useContext(SnackbarContext);

  useEffect(() => {
    if (newUser.role === "Admin") {
      setNewUser((prev) => ({ ...prev, departmentId: "5" }));
    } else if (newUser.role === "HR" || newUser.role === "HRManager") {
      setNewUser((prev) => ({ ...prev, departmentId: "9" }));
    } else if (newUser.role === "Employee") {
      setNewUser((prev) => ({ ...prev, departmentId: "" }));
    }
  }, [newUser.role]);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const { data } = await api.get(`/admin/users?pageSize=1000`);
      const filteredItems = data.items.slice(1);

      // Filter items based on search term
      const searchedItems = search
        ? filteredItems.filter(
            (item) =>
              item.email.toLowerCase().includes(search.toLowerCase()) ||
              item.fullName.toLowerCase().includes(search.toLowerCase()) ||
              item.role.toLowerCase().includes(search.toLowerCase()) ||
              (item.department &&
                item.department.toLowerCase().includes(search.toLowerCase()))
          )
        : filteredItems;

      // Calculate pagination
      const startIndex = page * pageSize;
      const endIndex = startIndex + pageSize;

      setUsers(searchedItems.slice(startIndex, endIndex));
      setTotalItems(searchedItems.length);
    } catch {
      showSnackbar("Failed loading users", "error");
    }
    setLoading(false);
  };

  const loadDepartments = async () => {
    setDepartments([
      { id: 1, name: "Engineering" },
      { id: 2, name: "Finance" },
      { id: 3, name: "Marketing" },
      { id: 4, name: "Sales" },
      { id: 5, name: "IT Support" },
      { id: 6, name: "Operations" },
      { id: 7, name: "R&D" },
      { id: 8, name: "Customer Service" },
      { id: 9, name: "HR" },
    ]);
  };

  useEffect(() => {
    loadUsers();
    loadDepartments();
  }, [page, pageSize, search]);

  const toggleActive = async (id, active) => {
    try {
      await api.put(`/admin/users/${id}/status?active=${!active}`);
      showSnackbar("Updated active status", "info");
      loadUsers();
    } catch {
      showSnackbar("Failed to update status", "error");
    }
  };

  const handleConfirm = async () => {
    if (!selectedUser) return;
    try {
      if (confirmAction === "reset") {
        await api.post(`/admin/users/${selectedUser.id}/reset-password`);
        showSnackbar("Temp password emailed", "success");
      } else if (confirmAction === "delete") {
        await api.delete(`/admin/users/${selectedUser.id}`);
        showSnackbar("User deleted", "warning");
      }
      loadUsers();
    } catch {
      showSnackbar("Action failed", "error");
    }
    setConfirmAction(null);
    setSelectedUser(null);
  };

  // ---------- VALIDATION FUNCTIONS ----------
  const validateEmail = (email) => {
    if (!email) return "Email is required";
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email) ? "" : "Invalid email format";
  };

  const validateName = (name) => {
    if (!name) return "Name is required";
    const nameRegex = /^[A-Za-z\s]+$/;
    return nameRegex.test(name)
      ? ""
      : "Name should contain only letters & spaces";
  };

  // Handle real-time validation
  const handleInputChange = (field, value) => {
    setNewUser((prev) => ({ ...prev, [field]: value }));

    if (field === "email") {
      setValidationErrors((prev) => ({
        ...prev,
        email: validateEmail(value),
      }));
    }
    if (field === "fullName") {
      setValidationErrors((prev) => ({
        ...prev,
        fullName: validateName(value),
      }));
    }
  };

  const createUser = async (e) => {
    e.preventDefault();
    const emailErr = validateEmail(newUser.email);
    const nameErr = validateName(newUser.fullName);

    if (emailErr || nameErr || !newUser.role || !newUser.departmentId) {
      showSnackbar("Please fix form errors before submitting", "error");
      return;
    }
    try {
      await api.post("/admin/users", newUser);
      showSnackbar("User created!", "success");
      setShowCreate(false);
      setNewUser({
        email: "",
        fullName: "",
        role: "",
        departmentId: "",
      });
      setValidationErrors({ email: "", fullName: "" });
      loadUsers();
    } catch {
      showSnackbar("Creation failed", "error");
    }
  };

  // Disable Create button logic
  const isCreateDisabled =
    !newUser.email ||
    !newUser.fullName ||
    !newUser.role ||
    !newUser.departmentId ||
    validationErrors.email ||
    validationErrors.fullName;

  return (
    <Box
      sx={{ p: 3, display: "flex", flexDirection: "column", height: "100%" }}
    >
      <Box display="flex" justifyContent="space-between" mb={2}>
        <Typography variant="h5">User Management</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => setShowCreate(true)}
        >
          Create User
        </Button>
      </Box>

      {/* Search */}
      <Box display="flex" gap={1} mb={2}>
        <TextField
          label="Search users"
          value={search}
          onChange={(e) => {
            setPage(0);
            setSearch(e.target.value);
          }}
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

      {/* Users Table */}
      <TableContainer component={Paper} sx={{ flex: 1 }}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Email</TableCell>
              <TableCell>Name</TableCell>
              <TableCell>Role</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Department</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={6} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center">
                  No users found.
                </TableCell>
              </TableRow>
            ) : (
              users.map((u) => (
                <TableRow key={u.id}>
                  <TableCell>{u.email}</TableCell>
                  <TableCell>{u.fullName}</TableCell>
                  <TableCell>{u.role}</TableCell>
                  <TableCell>
                    <Button
                      size="small"
                      variant={u.isActive ? "contained" : "outlined"}
                      color={u.isActive ? "success" : "inherit"}
                      onClick={() => toggleActive(u.id, u.isActive)}
                    >
                      {u.isActive ? "Active" : "Inactive"}
                    </Button>
                  </TableCell>
                  <TableCell>{u.department || "-"}</TableCell>
                  <TableCell align="center">
                    <Tooltip title="Reset Password">
                      <IconButton
                        color="warning"
                        onClick={() => {
                          setConfirmAction("reset");
                          setSelectedUser(u);
                        }}
                      >
                        <RestartAltIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete User">
                      <IconButton
                        color="error"
                        onClick={() => {
                          setConfirmAction("delete");
                          setSelectedUser(u);
                        }}
                      >
                        <DeleteIcon />
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
        labelDisplayedRows={({ from, to }) => `${from}–${to}`}
      />

      {/* Create User Modal */}
      <Dialog open={showCreate} onClose={() => setShowCreate(false)}>
        <DialogTitle>Create User</DialogTitle>
        <Box component="form" noValidate onSubmit={createUser}>
          <DialogContent>
            <TextField
              label="Email"
              fullWidth
              margin="normal"
              value={newUser.email}
              onChange={(e) => handleInputChange("email", e.target.value)}
              error={!!validationErrors.email}
              helperText={validationErrors.email}
            />
            <TextField
              label="Full Name"
              fullWidth
              margin="normal"
              value={newUser.fullName}
              onKeyDown={(e) => {
                if (/[0-9]/.test(e.key)) {
                  e.preventDefault();
                }
              }}
              onChange={(e) => handleInputChange("fullName", e.target.value)}
              error={!!validationErrors.fullName}
              helperText={validationErrors.fullName}
            />
            <Select
              fullWidth
              displayEmpty
              value={newUser.role}
              onChange={(e) => setNewUser({ ...newUser, role: e.target.value })}
              sx={{ mt: 2 }}
            >
              <MenuItem value="">Select Role</MenuItem>
              <MenuItem value="Admin">Admin</MenuItem>
              <MenuItem value="HR">HR</MenuItem>
              <MenuItem value="HRManager">HRManager</MenuItem>
              <MenuItem value="Employee">Employee</MenuItem>
            </Select>

            {newUser.role && (
              <Select
                fullWidth
                displayEmpty
                value={newUser.departmentId}
                onChange={(e) =>
                  setNewUser({ ...newUser, departmentId: e.target.value })
                }
                sx={{ mt: 2 }}
                disabled={newUser.role !== "Employee"}
                error={!newUser.departmentId}
              >
                <MenuItem value="">Select Department</MenuItem>
                {(newUser.role === "Employee"
                  ? departments.filter((d) => d.name !== "HR")
                  : departments.filter((d) =>
                      newUser.role === "Admin"
                        ? d.name === "IT Support"
                        : d.name === "HR"
                    )
                ).map((dept) => (
                  <MenuItem key={dept.id} value={dept.id}>
                    {dept.name}
                  </MenuItem>
                ))}
              </Select>
            )}
            {newUser.role && !newUser.departmentId && (
              <Typography variant="caption" color="error">
                Department is required
              </Typography>
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowCreate(false)}>Cancel</Button>
            <Button
              type="submit"
              variant="contained"
              disabled={isCreateDisabled}
            >
              Create
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Confirm Action Modal */}
      <Dialog open={!!confirmAction} onClose={() => setConfirmAction(null)}>
        <DialogTitle>
          {confirmAction === "reset" ? "Reset Password" : "Delete User"}
        </DialogTitle>
        <DialogContent>
          {selectedUser && (
            <Typography>
              Are you sure you want to{" "}
              <b>{confirmAction === "reset" ? "reset password" : "delete"}</b>{" "}
              for <span style={{ color: "blue" }}>{selectedUser.fullName}</span>
              ?
            </Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmAction(null)}>Cancel</Button>
          <Button
            onClick={handleConfirm}
            color={confirmAction === "reset" ? "warning" : "error"}
            variant="contained"
          >
            {confirmAction === "reset" ? "Confirm Reset" : "Confirm Delete"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default UserList;
