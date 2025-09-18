import {
  AppBar,
  Badge,
  Box,
  Button,
  Chip,
  IconButton,
  Menu,
  MenuItem,
  Toolbar,
  Typography,
} from "@mui/material";
import { Link, NavLink } from "react-router-dom";
import { useContext, useEffect, useState } from "react";

import { AuthContext } from "../context/AuthContext";
import { NotificationContext } from "../context/NotificationContext";
import LogoutIcon from "@mui/icons-material/Logout";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import NotificationsIcon from "@mui/icons-material/Notifications";
import PaidIcon from "@mui/icons-material/Paid";
import api from "../api/axiosClient";

function Navbar() {
  const { user, logout } = useContext(AuthContext);

  const notificationContext = useContext(NotificationContext);
  const unreadCount = notificationContext?.unreadCount ?? 0;

  const [menuAnchors, setMenuAnchors] = useState({});

  const handleMenuOpen = (menuKey) => (event) => {
    setMenuAnchors((prev) => ({ ...prev, [menuKey]: event.currentTarget }));
  };

  const handleMenuClose = (menuKey) => () => {
    setMenuAnchors((prev) => ({ ...prev, [menuKey]: null }));
  };

  const roleColors = {
    Admin: "error",
    HR: "primary",
    HRManager: "secondary",
    Employee: "success",
  };

  return (
    <AppBar
      position="sticky"
      style={{ backgroundColor: "teal" }}
      enableColorOnDark
    >
      <Toolbar sx={{ display: "flex", justifyContent: "space-between" }}>
        <Box sx={{ display: "flex", alignItems: "center" }}>
          <PaidIcon sx={{ mr: 1, color: "gold" }} />
          <Typography
            variant="h6"
            component={Link}
            to="/"
            style={{ textDecoration: "none", color: "inherit" }}
          >
            Payroll System
          </Typography>
        </Box>

        {user ? (
          <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
            <Typography variant="body1">
              Hi, <strong>{user.fullName}</strong>
            </Typography>
            <Chip
              label={user.role}
              color={roleColors[user.role] ?? "default"}
              size="small"
            />

            {}
            <Box sx={{ display: "flex", gap: 1 }}>
              {user.role === "Admin" && (
                <>
                  <Button component={NavLink} to="/admin/users" color="inherit">
                    Manage Users
                  </Button>
                  <Button component={NavLink} to="/admin/audit" color="inherit">
                    Audit Logs
                  </Button>
                </>
              )}

              {}

              {(user.role === "HRManager" || user.role === "HR") && (
                <>
                  <Button
                    color="inherit"
                    onClick={handleMenuOpen("payroll")}
                    endIcon={<MoreVertIcon />}
                  >
                    PayRoll
                  </Button>
                  <Menu
                    anchorEl={menuAnchors["payroll"]}
                    open={Boolean(menuAnchors["payroll"])}
                    onClose={handleMenuClose("payroll")}
                  >
                    <MenuItem
                      component={NavLink}
                      to="/hr/employees"
                      onClick={handleMenuClose("payroll")}
                    >
                      Employee
                    </MenuItem>
                    <MenuItem
                      component={NavLink}
                      to="/hr/ctc"
                      onClick={handleMenuClose("payroll")}
                    >
                      Create CTC
                    </MenuItem>
                    <MenuItem
                      component={NavLink}
                      to="/hr/payslip"
                      onClick={handleMenuClose("payroll")}
                    >
                      Generate PaySlip
                    </MenuItem>
                    {user.role === "HR" && (
                      <MenuItem
                        component={NavLink}
                        to="/hr/exports"
                        onClick={handleMenuClose("payroll")}
                      >
                        Exports
                      </MenuItem>
                    )}
                  </Menu>
                </>
              )}

              {user.role === "HRManager" && (
                <>
                  <Button
                    color="inherit"
                    onClick={handleMenuOpen("manager")}
                    endIcon={<MoreVertIcon />}
                  >
                    Management
                  </Button>
                  <Menu
                    anchorEl={menuAnchors["manager"]}
                    open={Boolean(menuAnchors["manager"])}
                    onClose={handleMenuClose("manager")}
                  >
                    <MenuItem
                      component={NavLink}
                      to="/manager/approvals"
                      onClick={handleMenuClose("manager")}
                    >
                      Approvals
                    </MenuItem>
                    <MenuItem
                      component={NavLink}
                      to="/manager/analytics"
                      onClick={handleMenuClose("manager")}
                    >
                      Analytics
                    </MenuItem>
                    <MenuItem
                      component={NavLink}
                      to="/manager/audit"
                      onClick={handleMenuClose("manager")}
                    >
                      Audit Logs
                    </MenuItem>
                  </Menu>
                </>
              )}

              {(user.role === "Employee" ||
                user.role === "HR" ||
                user.role === "HRManager") && (
                <>
                  <Button
                    color="inherit"
                    onClick={handleMenuOpen("profile")}
                    endIcon={<MoreVertIcon />}
                  >
                    Profile
                  </Button>
                  <Menu
                    anchorEl={menuAnchors["profile"]}
                    open={Boolean(menuAnchors["profile"])}
                    onClose={handleMenuClose("profile")}
                  >
                    <MenuItem
                      component={NavLink}
                      to="/employee/profile"
                      onClick={handleMenuClose("profile")}
                    >
                      My Profile
                    </MenuItem>
                    <MenuItem
                      component={NavLink}
                      to="/employee/payslips"
                      onClick={handleMenuClose("profile")}
                    >
                      My Payslips
                    </MenuItem>
                    <MenuItem
                      component={NavLink}
                      to="/employee/compare"
                      onClick={handleMenuClose("profile")}
                    >
                      Compare
                    </MenuItem>
                  </Menu>
                </>
              )}
            </Box>

            {}
            {user && (
              <IconButton
                component={NavLink}
                to="/notifications"
                color="inherit"
              >
                <Badge badgeContent={unreadCount} color="error" showZero>
                  <NotificationsIcon />
                </Badge>
              </IconButton>
            )}
            <Button
              onClick={logout}
              variant="outlined"
              color="inherit"
              startIcon={<LogoutIcon />}
            >
              Logout
            </Button>
          </Box>
        ) : (
          <Box>
            <Button
              component={Link}
              to="/login"
              variant="outlined"
              color="inherit"
            >
              Login
            </Button>
          </Box>
        )}
      </Toolbar>
    </AppBar>
  );
}

export default Navbar;
