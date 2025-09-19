import {
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  TextField,
  Typography,
} from "@mui/material";
import React, { useContext, useState } from "react";
import { useNavigate } from "react-router-dom";

import { AuthContext } from "../context/AuthContext";
import { SnackbarContext } from "../context/SnackbarProvider"; // ✅ renamed context
import api from "../api/axiosClient";

function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const { login } = useContext(AuthContext);
  const showSnackbar = useContext(SnackbarContext); // ✅ global call only

  const [showForgot, setShowForgot] = useState(false);
  const [forgotEmail, setForgotEmail] = useState("");

  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();

    if (!email) {
      showSnackbar("Email is required", "error");
      return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      showSnackbar("Enter a valid email address", "error");
      return;
    }
    if (!password) {
      showSnackbar("Password is required", "error");
      return;
    }

    try {
      const { data } = await api.post("/auth/login", { email, password });
      login(data);
      showSnackbar("Welcome " + data.fullName, "success");
      navigate("/");
    } catch (err) {
      if (err.response?.status === 500) {
        showSnackbar("Server error. Please try again later.", "error");
      } else {
        showSnackbar("Invalid credentials", "error");
      }
    }
  };

  const handleForgotSubmit = async (e) => {
    e.preventDefault();
    if (!forgotEmail || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(forgotEmail)) {
      showSnackbar("Enter a valid email address", "error");
      return;
    }
    try {
      await api.post("/auth/forgot-password/request", { email: forgotEmail });
      showSnackbar(
        "Reset request submitted. Please check your email.",
        "success"
      );
      setShowForgot(false);
      setForgotEmail("");
    } catch (err) {
      if (err.response?.status === 500) {
        showSnackbar("Server error. Please try again later.", "error");
      } else {
        showSnackbar("Failed to submit reset request", "error");
      }
    }
  };

  return (
    <Grid
      container
      justifyContent="center"
      alignItems="center"
      sx={{ minHeight: "80vh" }}
      data-testid="login-page"
    >
      <Grid item xs={10} sm={6} md={4}>
        <Card elevation={4}>
          <CardContent>
            <Typography
              variant="h4"
              align="center"
              gutterBottom
              data-testid="login-title"
            >
              Payroll Dashboard Login
            </Typography>

            <Box
              component="form"
              noValidate
              onSubmit={handleLogin}
              data-testid="login-form"
            >
              <TextField
                label="Email"
                type="text"
                fullWidth
                margin="normal"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                inputProps={{ "data-testid": "email-input" }}
              />
              <TextField
                label="Password"
                type="password"
                fullWidth
                margin="normal"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                inputProps={{ "data-testid": "password-input" }}
              />
              <Button
                type="submit"
                variant="contained"
                color="primary"
                fullWidth
                sx={{ mt: 2 }}
                data-testid="login-button"
              >
                Login
              </Button>
            </Box>

            <Box mt={2} display="flex" justifyContent="space-between">
              <Button
                variant="text"
                onClick={() => setShowForgot(true)}
                data-testid="forgot-password-button"
              >
                Forgot Password?
              </Button>
            </Box>
          </CardContent>
        </Card>
      </Grid>

      <Dialog
        fullWidth
        maxWidth="sm"
        open={showForgot}
        onClose={() => setShowForgot(false)}
        data-testid="forgot-password-dialog"
      >
        <DialogTitle data-testid="forgot-password-title">
          Forgot Password
        </DialogTitle>
        <Box
          component="form"
          noValidate
          onSubmit={handleForgotSubmit}
          data-testid="forgot-password-form"
        >
          <DialogContent>
            <TextField
              label="Email"
              type="text"
              fullWidth
              margin="normal"
              value={forgotEmail}
              onChange={(e) => setForgotEmail(e.target.value)}
              inputProps={{ "data-testid": "forgot-email-input" }}
            />
          </DialogContent>
          <DialogActions>
            <Button
              onClick={() => setShowForgot(false)}
              color="secondary"
              data-testid="cancel-forgot-button"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="contained"
              color="primary"
              data-testid="submit-forgot-button"
            >
              Submit Request
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Grid>
  );
}
export default Login;
