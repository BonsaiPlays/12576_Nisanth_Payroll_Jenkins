import "react-toastify/dist/ReactToastify.css";

import { BrowserRouter, Route, Routes } from "react-router-dom";

import Analytics from "./pages/Manager/Analytics";
import Approvals from "./pages/Manager/Approvals";
import AuditLogs from "./pages/AuditLogs";
import { AuthProvider } from "./context/AuthContext";
import { NotificationProvider } from "./context/NotificationContext";
import { SnackbarProvider } from "./context/SnackbarProvider";
import { Box } from "@mui/material";
import CTCForm from "./pages/HR/CTCForm";
import Compare from "./pages/Employee/Compare";
import Dashboard from "./pages/Dashboard";
import Employees from "./pages/HR/Employees";
import Exports from "./pages/HR/Exports";
import Login from "./pages/Login";
import Navbar from "./components/Navbar";
import Notifications from "./pages/Notifications";
import PayslipForm from "./pages/HR/PayslipForm";
import PayslipList from "./pages/Employee/PayslipList";
import PrivateRoute from "./components/PrivateRoute";
import Profile from "./pages/Employee/Profile";
import { ToastContainer } from "react-toastify";
import UserList from "./pages/Admin/UserList";


function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <NotificationProvider>
          <SnackbarProvider>
            <Box
              sx={{ height: "100vh", display: "flex", flexDirection: "column" }}
            >
              <Navbar />
              <Box component="main" sx={{ flex: 1, overflow: "auto", p: 2 }}>
                <Routes>
                  <Route path="/login" element={<Login />} />
                  <Route
                    path="/"
                    element={
                      <PrivateRoute>
                        <Dashboard />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/notifications"
                    element={
                      <PrivateRoute
                        roles={["Employee", "HR", "HRManager", "Admin"]}
                      >
                        <Notifications />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/employee/profile"
                    element={
                      <PrivateRoute roles={["Employee", "HR", "HRManager"]}>
                        <Profile />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/employee/payslips"
                    element={
                      <PrivateRoute roles={["Employee", "HR", "HRManager"]}>
                        <PayslipList />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/employee/compare"
                    element={
                      <PrivateRoute roles={["Employee", "HR", "HRManager"]}>
                        <Compare />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/admin/users"
                    element={
                      <PrivateRoute roles={["Admin"]}>
                        <UserList />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/admin/audit"
                    element={
                      <PrivateRoute roles={["Admin"]}>
                        <AuditLogs role="Admin" />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/hr/employees"
                    element={
                      <PrivateRoute roles={["HR", "HRManager"]}>
                        <Employees />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/hr/ctc"
                    element={
                      <PrivateRoute roles={["HR", "HRManager"]}>
                        <CTCForm />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/hr/payslip"
                    element={
                      <PrivateRoute roles={["HR", "HRManager"]}>
                        <PayslipForm />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/hr/exports"
                    element={
                      <PrivateRoute roles={["HR"]}>
                        <Exports />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/manager/approvals"
                    element={
                      <PrivateRoute roles={["HRManager"]}>
                        <Approvals />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/manager/analytics"
                    element={
                      <PrivateRoute roles={["HRManager"]}>
                        <Analytics />
                      </PrivateRoute>
                    }
                  />
                  <Route
                    path="/manager/audit"
                    element={
                      <PrivateRoute roles={["HRManager"]}>
                        <AuditLogs role="HRManager" />
                      </PrivateRoute>
                    }
                  />
                </Routes>
              </Box>
              <ToastContainer />
            </Box>
          </SnackbarProvider>
        </NotificationProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
