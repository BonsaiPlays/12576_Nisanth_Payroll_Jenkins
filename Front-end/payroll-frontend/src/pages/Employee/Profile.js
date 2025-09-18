import React, { useEffect, useState, useContext } from "react";
import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Chip,
  TextField,
  Typography,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableContainer,
  Paper,
} from "@mui/material";
import { useNavigate } from "react-router-dom";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { PickersDay } from "@mui/x-date-pickers/PickersDay";
import { Badge } from "@mui/material";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DateCalendar } from "@mui/x-date-pickers/DateCalendar";
import dayjs from "dayjs";
import { formatDate } from "../../utils/date";

import VisibilityIcon from "@mui/icons-material/Visibility";
import DownloadIcon from "@mui/icons-material/Download";

import api from "../../api/axiosClient";
import { SnackbarContext } from "../../context/SnackbarProvider"; // ✅  use global snackbar

function Profile() {
  const [profile, setProfile] = useState(null);
  const [errors, setErrors] = useState({});
  const [showEditModal, setShowEditModal] = useState(false);
  const [showCtcModal, setShowCtcModal] = useState(false);
  const [showPasswordModal, setShowPasswordModal] = useState(false);

  const [tempAddress, setTempAddress] = useState("");
  const [tempPhone, setTempPhone] = useState("");

  const [ctcs, setCtcs] = useState([]);
  const [selectedCtc, setSelectedCtc] = useState(null);

  const [currentPw, setCurrentPw] = useState("");
  const [newPw, setNewPw] = useState("");
  const [confirmPw, setConfirmPw] = useState("");

  const [memos, setMemos] = useState([]);
  const [selectedDate, setSelectedDate] = useState(null);
  const [selectedMemo, setSelectedMemo] = useState(null);
  const [showMemoModal, setShowMemoModal] = useState(false);
  const [memoText, setMemoText] = useState("");

  const navigate = useNavigate();
  const showSnackbar = useContext(SnackbarContext);

  const load = async () => {
    try {
      const { data } = await api.get("/employee/profile");
      setProfile(data);

      const { data: ctcList } = await api.get("/employee/ctcs");
      setCtcs(ctcList);

      const { data: memosData } = await api.get("/memos");
      setMemos(memosData);
    } catch {
      showSnackbar("Failed loading profile or CTCs", "error");
    }
  };

  useEffect(() => {
    load();
  }, []);

  const renderDayWithMemo = (props) => {
    const { day, ...others } = props;
    const dateStr = day.format("YYYY-MM-DD");
    const hasMemo = memos.some((m) => m.date.startsWith(dateStr));

    return (
      <Badge
        key={dateStr}
        overlap="circular"
        badgeContent={hasMemo ? "📝" : null}
        title={
          hasMemo ? memos.find((m) => m.date.startsWith(dateStr)).content : ""
        }
      >
        <PickersDay {...others} day={day} />
      </Badge>
    );
  };

  const handleDateClick = (date) => {
    const dateStr = date.format("YYYY-MM-DD");
    const existing = memos.find((m) => m.date.startsWith(dateStr));
    setSelectedDate(dateStr);
    setSelectedMemo(existing || null);
    setMemoText(existing?.content || "");
    setShowMemoModal(true);
  };

  // ---- VALIDATIONS ----
  const validateProfileForm = () => {
    const errs = {};
    if (!tempAddress.trim()) {
      errs.address = "Address is required";
    } else if (tempAddress.length < 5) {
      errs.address = "Address must be at least 5 characters";
    }
    if (!tempPhone.trim()) {
      errs.phone = "Phone is required";
    } else if (!/^[0-9]+$/.test(tempPhone)) {
      errs.phone = "Phone must contain digits only";
    } else if (tempPhone.length < 7 || tempPhone.length > 15) {
      errs.phone = "Phone must be 7–15 digits long";
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const profileIncomplete =
    !profile?.profile?.address || !profile?.profile?.phone;

  const validatePassword = () => {
    const requirements = {
      length: newPw.length >= 8,
      uppercase: /[A-Z]/.test(newPw),
      number: /[0-9]/.test(newPw),
      match: newPw === confirmPw && newPw !== "",
    };
    return requirements;
  };

  // ---- ACTIONS ----
  const saveProfile = async () => {
    if (!validateProfileForm()) return;
    try {
      await api.put("/employee/profile", {
        address: tempAddress,
        phone: tempPhone,
      });
      showSnackbar("Profile updated!", "success");
      setShowEditModal(false);
      setTimeout(load, 1000); // delay so snackbar is visible before refresh
    } catch {
      showSnackbar("Update failed", "error");
    }
  };

  const changePassword = async () => {
    const checks = validatePassword();
    const allValid = Object.values(checks).every(Boolean);
    if (!allValid) {
      showSnackbar("Fix password requirements first", "error");
      return;
    }
    try {
      await api.put("/auth/password", {
        currentPassword: currentPw,
        newPassword: newPw,
      });
      showSnackbar("Password changed successfully!", "success");
      setShowPasswordModal(false);
      setCurrentPw("");
      setNewPw("");
      setConfirmPw("");
    } catch {
      showSnackbar("Password change failed", "error");
    }
  };

  const downloadCtc = async (id, dateStr) => {
    try {
      const res = await api.get(`/employee/ctcs/${id}/pdf`, {
        responseType: "blob",
      });
      const url = URL.createObjectURL(
        new Blob([res.data], { type: "application/pdf" })
      );
      const link = document.createElement("a");
      link.href = url;
      link.download = `MyCTC_${dateStr}.pdf`;
      link.click();
      link.remove();
    } catch {
      showSnackbar("Download failed", "error");
    }
  };

  if (!profile) {
    return (
      <Box textAlign="center" mt={5}>
        <CircularProgress />
        <Typography mt={2}>Loading profile...</Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3, display: "flex", flexDirection: "column", gap: 3 }}>
      {profileIncomplete && (
        <Alert severity="warning">
          ⚠️ Your profile is incomplete. Please update your contact info.
        </Alert>
      )}

      {/* Identity */}
      <Card>
        <CardContent
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
          }}
        >
          <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
            <Typography variant="h3">👤</Typography>
            <Box>
              <Typography variant="h6">{profile.fullName}</Typography>
              <Typography variant="body2" color="text.secondary">
                {profile.email}
              </Typography>
              <Chip
                label={profile.role}
                color="secondary"
                size="small"
                sx={{ mt: 1 }}
              />
            </Box>
          </Box>
          <Button variant="outlined" onClick={() => setShowPasswordModal(true)}>
            Change Password
          </Button>
        </CardContent>
      </Card>

      {/* Contact Info */}
      <Card>
        <CardHeader
          title="Contact Info"
          action={
            <Button
              size="small"
              variant="outlined"
              onClick={() => {
                setTempAddress(profile.profile?.address || "");
                setTempPhone(profile.profile?.phone || "");
                setShowEditModal(true);
              }}
            >
              Edit
            </Button>
          }
        />
        <CardContent>
          <Typography>
            <b>Address:</b> {profile.profile?.address || "Not provided"}
          </Typography>
          <Typography>
            <b>Phone:</b> {profile.profile?.phone || "Not provided"}
          </Typography>
          <Typography>
            <b>Department:</b> {profile.profile?.department || "Not Assigned"}{" "}
            <Typography component="span" color="text.secondary">
              (Managed by HR)
            </Typography>
          </Typography>
        </CardContent>
      </Card>

      {/* Action Shortcuts */}
      <Box sx={{ display: "flex", gap: 2 }}>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Button
              fullWidth
              variant="contained"
              onClick={() => navigate("/employee/payslips")}
            >
              View My Payslips
            </Button>
          </CardContent>
        </Card>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Button
              fullWidth
              variant="contained"
              onClick={() => setShowCtcModal(true)}
            >
              View My CTC History
            </Button>
          </CardContent>
        </Card>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Button
              fullWidth
              variant="contained"
              onClick={() => navigate("/employee/compare")}
            >
              Compare CTC and Payslips
            </Button>
          </CardContent>
        </Card>
      </Box>

      {/* Calendar */}
      <Card>
        <CardHeader title="Calendar" />
        <CardContent>
          <LocalizationProvider dateAdapter={AdapterDayjs}>
            <DateCalendar
              disablePast
              value={dayjs()}
              onChange={(newValue) => handleDateClick(newValue)}
              slots={{
                day: (props) => renderDayWithMemo(props),
              }}
            />
          </LocalizationProvider>
        </CardContent>
      </Card>

      {/* --- Modals --- */}

      {/* Edit Profile */}
      <Dialog
        open={showEditModal}
        onClose={() => setShowEditModal(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Edit Contact Info</DialogTitle>
        <DialogContent dividers>
          <TextField
            fullWidth
            margin="normal"
            label="Address"
            value={tempAddress}
            onChange={(e) => {
              setTempAddress(e.target.value);
              validateProfileForm();
            }}
            error={!!errors.address}
            helperText={errors.address}
          />
          <TextField
            fullWidth
            margin="normal"
            label="Phone"
            value={tempPhone}
            onChange={(e) => {
              setTempPhone(e.target.value);
              validateProfileForm();
            }}
            error={!!errors.phone}
            helperText={errors.phone}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowEditModal(false)}>Cancel</Button>
          <Button variant="contained" onClick={saveProfile}>
            Save
          </Button>
        </DialogActions>
      </Dialog>

      {/* Change Password */}
      <Dialog
        open={showPasswordModal}
        onClose={() => setShowPasswordModal(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Change Password</DialogTitle>
        <DialogContent dividers>
          <TextField
            fullWidth
            margin="normal"
            type="password"
            label="Current Password"
            value={currentPw}
            onChange={(e) => setCurrentPw(e.target.value)}
          />
          <TextField
            fullWidth
            margin="normal"
            type="password"
            label="New Password"
            value={newPw}
            onChange={(e) => setNewPw(e.target.value)}
          />
          <TextField
            fullWidth
            margin="normal"
            type="password"
            label="Confirm Password"
            value={confirmPw}
            onChange={(e) => setConfirmPw(e.target.value)}
          />
          {(() => {
            const checks = validatePassword();
            return (
              <Box sx={{ mt: 1 }}>
                <Typography
                  variant="body2"
                  color={checks.length ? "success.main" : "error.main"}
                >
                  • At least 8 characters
                </Typography>
                <Typography
                  variant="body2"
                  color={checks.uppercase ? "success.main" : "error.main"}
                >
                  • Contains uppercase
                </Typography>
                <Typography
                  variant="body2"
                  color={checks.number ? "success.main" : "error.main"}
                >
                  • Contains number
                </Typography>
                <Typography
                  variant="body2"
                  color={checks.match ? "success.main" : "error.main"}
                >
                  • Passwords match
                </Typography>
              </Box>
            );
          })()}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowPasswordModal(false)}>Cancel</Button>
          <Button variant="contained" onClick={changePassword}>
            Update
          </Button>
        </DialogActions>
      </Dialog>

      {/* CTC History Modal */}
      <Dialog
        open={showCtcModal}
        onClose={() => setShowCtcModal(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>CTC History</DialogTitle>
        <DialogContent dividers>
          {ctcs.length === 0 ? (
            <Typography>No CTC records found.</Typography>
          ) : (
            <TableContainer component={Paper}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Effective From</TableCell>
                    <TableCell>Tax %</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {ctcs.map((c) => (
                    <TableRow key={c.id} hover>
                      <TableCell>{formatDate(c.effectiveFrom)}</TableCell>
                      <TableCell>{c.taxPercent}%</TableCell>
                      <TableCell>
                        <Chip
                          label={c.status}
                          size="small"
                          color={
                            c.status === "Approved"
                              ? "success"
                              : c.status === "Rejected"
                              ? "error"
                              : "warning"
                          }
                        />
                      </TableCell>
                      <TableCell>
                        <IconButton onClick={() => setSelectedCtc(c)}>
                          <VisibilityIcon />
                        </IconButton>
                        <IconButton
                          onClick={() =>
                            downloadCtc(
                              c.id,
                              new Date(c.effectiveFrom).toISOString().split("T")[0]
                            )
                          }
                        >
                          <DownloadIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowCtcModal(false)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Memo Modal */}
      <Dialog open={showMemoModal} onClose={() => setShowMemoModal(false)}>
        <DialogTitle>Memo for {formatDate(selectedDate)}</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            multiline
            rows={4}
            value={memoText}
            onChange={(e) => setMemoText(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          {selectedMemo && (
            <Button
              color="error"
              onClick={async () => {
                await api.delete(`/memos/${selectedMemo.id}`);
                setMemos(memos.filter((m) => m.id !== selectedMemo.id));
                showSnackbar("Memo deleted", "success");
                setShowMemoModal(false);
              }}
            >
              Delete
            </Button>
          )}
          <Button onClick={() => setShowMemoModal(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={async () => {
              if (selectedMemo) {
                const res = await api.put(`/memos/${selectedMemo.id}`, {
                  content: memoText,
                  date: selectedDate,
                });
                setMemos(memos.map((m) => (m.id === res.data.id ? res.data : m)));
                showSnackbar("Memo updated", "success");
              } else {
                const res = await api.post(`/memos`, {
                  content: memoText,
                  date: selectedDate,
                });
                setMemos([...memos, res.data]);
                showSnackbar("Memo created", "success");
              }
              setShowMemoModal(false);
            }}
          >
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default Profile;