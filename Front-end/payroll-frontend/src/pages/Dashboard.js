import { Link } from "react-router-dom";
import React, { useContext } from "react";
import { AuthContext } from "../context/AuthContext";
import {
  Box,
  Grid,
  Typography,
  Card,
  CardContent,
  CardActionArea,
} from "@mui/material";

import ShieldIcon from "@mui/icons-material/Security";
import PeopleIcon from "@mui/icons-material/People";
import BadgeIcon from "@mui/icons-material/Badge";
import PersonIcon from "@mui/icons-material/Person";
import ListAltIcon from "@mui/icons-material/ListAlt";
import MonetizationOnIcon from "@mui/icons-material/MonetizationOn";
import ReceiptIcon from "@mui/icons-material/Receipt";
import InsertChartIcon from "@mui/icons-material/InsertChart";
import NotificationsActiveIcon from "@mui/icons-material/Notifications";
import FileDownloadIcon from "@mui/icons-material/FileDownload";

function Dashboard() {
  const { user } = useContext(AuthContext);
  if (!user) return <Box p={5}>Please login</Box>;

  const roleColors = {
    Admin: "error.main",
    HR: "primary.main",
    HRManager: "secondary.main",
    Employee: "success.main",
  };

  const cards = {
    Admin: [
      {
        to: "/admin/users",
        title: "Manage Users",
        text: "Create users, assign roles, reset passwords",
        icon: (
          <ShieldIcon
            sx={{ fontSize: 40 }}
            color="error"
            data-testid="manage-users-icon"
          />
        ),
      },
      {
        to: "/admin/audit",
        title: "Audit Logs",
        text: "System actions history",
        icon: (
          <ListAltIcon
            sx={{ fontSize: 40 }}
            color="error"
            data-testid="audit-logs-icon"
          />
        ),
      },
    ],
    HR: [
      {
        to: "/hr/employees",
        title: "Employees",
        text: "Browse and manage employees",
        icon: (
          <PeopleIcon
            sx={{ fontSize: 40 }}
            color="primary"
            data-testid="employees-icon"
          />
        ),
      },
      {
        to: "/hr/ctc",
        title: "Create CTC",
        text: "Define salary structures",
        icon: (
          <MonetizationOnIcon
            sx={{ fontSize: 40 }}
            color="primary"
            data-testid="ctc-creation-icon"
          />
        ),
      },
      {
        to: "/hr/payslip",
        title: "Generate Payslip",
        text: "Generate payroll slips w/ LOP",
        icon: (
          <ReceiptIcon
            sx={{ fontSize: 40 }}
            color="primary"
            data-testid="generate-payslip-icon"
          />
        ),
      },
      {
        to: "/hr/exports",
        title: "Exports",
        text: "Export salary reports",
        icon: (
          <FileDownloadIcon
            sx={{ fontSize: 40 }}
            color="primary"
            data-testid="exports-icon"
          />
        ),
      },
    ],
    HRManager: [
      {
        to: "/hr/employees",
        title: "Employees",
        text: "Browse and manage employees",
        icon: (
          <PeopleIcon
            sx={{ fontSize: 40 }}
            color="secondary"
            data-testid="manager-employees-icon"
          />
        ),
      },
      {
        to: "/manager/approvals",
        title: "Approvals",
        text: "Approve or release CTCs & payslips",
        icon: (
          <BadgeIcon
            sx={{ fontSize: 40 }}
            color="secondary"
            data-testid="approvals-icon"
          />
        ),
      },
      {
        to: "/manager/analytics",
        title: "Analytics",
        text: "Payroll trends & reports",
        icon: (
          <InsertChartIcon
            sx={{ fontSize: 40 }}
            color="secondary"
            data-testid="analytics-icon"
          />
        ),
      },
      {
        to: "/hr/ctc",
        title: "Create CTC",
        text: "Define salary structures",
        icon: (
          <MonetizationOnIcon
            sx={{ fontSize: 40 }}
            color="secondary"
            data-testid="manager-create-ctc-icon"
          />
        ),
      },
      {
        to: "/hr/payslip",
        title: "Generate Payslip",
        text: "Create employee payslips manually",
        icon: (
          <ReceiptIcon
            sx={{ fontSize: 40 }}
            color="secondary"
            data-testid="manager-generate-payslip-icon"
          />
        ),
      },
      {
        to: "/manager/audit",
        title: "Audit Logs",
        text: "CTC and Payslip action logs",
        icon: (
          <ListAltIcon
            sx={{ fontSize: 40 }}
            color="secondary"
            data-testid="manager-audit-logs-icon"
          />
        ),
      },
    ],
    Employee: [
      {
        to: "/employee/profile",
        title: "Profile",
        text: "Manage personal info & CTC",
        icon: (
          <PersonIcon
            sx={{ fontSize: 40 }}
            color="success"
            data-testid="profile-icon"
          />
        ),
      },
      {
        to: "/employee/payslips",
        title: "Payslips",
        text: "View & download slips",
        icon: (
          <ReceiptIcon
            sx={{ fontSize: 40 }}
            color="success"
            data-testid="payslips-icon"
          />
        ),
      },
      {
        to: "/employee/compare",
        title: "Compare",
        text: "Compare salaries across months",
        icon: (
          <InsertChartIcon
            sx={{ fontSize: 40 }}
            color="success"
            data-testid="compare-icon"
          />
        ),
      },
      {
        to: "/employee/notifications",
        title: "Notifications",
        text: "Messages & payroll alerts",
        icon: (
          <NotificationsActiveIcon
            sx={{ fontSize: 40 }}
            color="success"
            data-testid="notifications-icon"
          />
        ),
      },
    ],
  };

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
      }}
      data-testid="dashboard-page"
    >
      <Typography variant="h5" gutterBottom data-testid="welcome-message">
        Welcome,{" "}
        <span
          style={{ color: roleColors[user.role] }}
          data-testid="user-fullname"
        >
          {user.fullName}
        </span>{" "}
        <Typography
          component="span"
          color="text.secondary"
          data-testid="user-role"
        >
          ({user.role})
        </Typography>
      </Typography>

      <Grid
        container
        spacing={3}
        maxWidth="lg"
        justifyContent="center"
        data-testid="dashboard-cards-container"
      >
        {cards[user.role].map((c) => {
          const cardTestId = c.title.toLowerCase().replace(/\s+/g, "-");
          return (
            <Grid
              item
              xs={12}
              sm={6}
              md={4}
              key={c.to}
              data-testid={`${cardTestId}-grid-item`}
            >
              <Card sx={{ height: "100%" }} data-testid={`${cardTestId}-card`}>
                <CardActionArea
                  component={Link}
                  to={c.to}
                  sx={{ height: "100%" }}
                  data-testid={`${cardTestId}-link`}
                >
                  <CardContent sx={{ textAlign: "center" }}>
                    {React.cloneElement(c.icon, {
                      "data-testid": `${cardTestId}-icon`,
                    })}
                    <Typography
                      variant="h6"
                      mt={2}
                      data-testid={`${cardTestId}-title`}
                    >
                      {c.title}
                    </Typography>
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      data-testid={`${cardTestId}-description`}
                    >
                      {c.text}
                    </Typography>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          );
        })}
      </Grid>
    </Box>
  );
}

export default Dashboard;
