import React, { useState, useEffect, useContext } from "react";
import api from "../api/axiosClient";
import { AuthContext } from "../context/AuthContext";
import { NotificationContext } from "../context/NotificationContext";
import { SnackbarContext } from "../context/SnackbarProvider";
import { formatDateTime } from "../utils/date";

import {
  Box,
  Typography,
  Button,
  TextField,
  List,
  ListItem,
  ListItemText,
  Chip,
  CircularProgress,
  Tooltip,
  IconButton,
  Switch,
  FormControlLabel,
  TablePagination,
} from "@mui/material";
import NotificationsIcon from "@mui/icons-material/Notifications";
import DoneAllIcon from "@mui/icons-material/DoneAll";
import DoneIcon from "@mui/icons-material/Done";

function Notifications() {
  const { user } = useContext(AuthContext);
  const [items, setItems] = useState([]);
  const [allItems, setAllItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const [showUnread, setShowUnread] = useState(false);

  // Pagination state
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [total, setTotal] = useState(0);

  const notificationContext = useContext(NotificationContext);
  const { loadUnread } = notificationContext || {};

  const showSnackbar = useContext(SnackbarContext);

  // Load notifications
  const load = async () => {
    setLoading(true);
    try {
      const { data } = await api.get("/notifications?pageSize=1000");
      const allNotifications = data.items || [];
      setAllItems(allNotifications);

      // Filter based on search and unread status
      let filteredItems = allNotifications;

      if (search) {
        filteredItems = filteredItems.filter(
          (item) =>
            item.subject?.toLowerCase().includes(search.toLowerCase()) ||
            item.message?.toLowerCase().includes(search.toLowerCase())
        );
      }

      if (showUnread) {
        filteredItems = filteredItems.filter((item) => !item.isRead);
      }

      // Calculate pagination
      const startIndex = page * pageSize;
      const endIndex = startIndex + pageSize;

      setItems(filteredItems.slice(startIndex, endIndex));
      setTotal(filteredItems.length);
    } catch {
      showSnackbar("Failed to load notifications", "error");
    }
    setLoading(false);
  };

  useEffect(() => {
    load();
  }, []); // Only load once initially

  // Effect for filtering and pagination
  useEffect(() => {
    if (allItems.length > 0) {
      let filteredItems = allItems;

      if (search) {
        filteredItems = filteredItems.filter(
          (item) =>
            item.subject?.toLowerCase().includes(search.toLowerCase()) ||
            item.message?.toLowerCase().includes(search.toLowerCase())
        );
      }

      if (showUnread) {
        filteredItems = filteredItems.filter((item) => !item.isRead);
      }

      const startIndex = page * pageSize;
      const endIndex = startIndex + pageSize;

      setItems(filteredItems.slice(startIndex, endIndex));
      setTotal(filteredItems.length);
    }
  }, [page, pageSize, search, showUnread, allItems]);

  const markAsRead = async (id) => {
    try {
      await api.put(`/notifications/${id}/read`);
      setAllItems((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      );
      loadUnread?.(); // update navbar badge count
      showSnackbar("Notification marked as read", "success");
    } catch {
      showSnackbar("Could not mark as read", "error");
    }
  };

  const markAllAsRead = async () => {
    try {
      await api.put(`/notifications/read-all`);
      setAllItems((prev) => prev.map((n) => ({ ...n, isRead: true })));
      showSnackbar("All notifications marked as read", "success");
      loadUnread?.();
    } catch {
      showSnackbar("Failed to mark all as read", "error");
    }
  };

  return (
    <Box
      sx={{ p: 3, height: "100%", display: "flex", flexDirection: "column" }}
    >
      {/* Header */}
      <Box
        display="flex"
        justifyContent="space-between"
        alignItems="center"
        mb={2}
      >
        <Typography variant="h5" display="flex" alignItems="center" gap={1}>
          <NotificationsIcon color="warning" /> Notifications
        </Typography>
        <Tooltip title="Mark all as read">
          <Button
            variant="outlined"
            startIcon={<DoneAllIcon />}
            onClick={markAllAsRead}
          >
            Mark All
          </Button>
        </Tooltip>
      </Box>

      {/* Search + Unread Only Toggle */}
      <Box display="flex" alignItems="center" gap={2} mb={2}>
        <TextField
          label="Search notifications"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(0);
          }}
          fullWidth
        />
        <FormControlLabel
          control={
            <Switch
              checked={showUnread}
              onChange={(e) => {
                setPage(0);
                setShowUnread(e.target.checked);
              }}
              color="primary"
            />
          }
          label="Unread Only"
        />
      </Box>

      {/* Notifications List */}
      {loading ? (
        <Box display="flex" justifyContent="center" mt={3}>
          <CircularProgress />
        </Box>
      ) : items.length === 0 ? (
        <Typography color="text.secondary" align="center">
          No notifications found.
        </Typography>
      ) : (
        <>
          <List sx={{ flex: 1, overflow: "auto" }}>
            {items.map((n) => (
              <ListItem
                key={n.id}
                divider
                sx={{ bgcolor: n.isRead ? "inherit" : "action.hover" }}
              >
                <ListItemText
                  primary={
                    <Box display="flex" alignItems="center">
                      <Typography variant="subtitle1">{n.subject}</Typography>
                      {!n.isRead && (
                        <Chip
                          label="New"
                          color="primary"
                          size="small"
                          sx={{ ml: 1 }}
                        />
                      )}
                    </Box>
                  }
                  secondary={
                    <>
                      <Typography variant="body2" color="text.secondary">
                        {n.message}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {formatDateTime(n.createdAt)}
                      </Typography>
                    </>
                  }
                />
                {!n.isRead && (
                  <Tooltip title="Mark as read">
                    <IconButton
                      color="success"
                      onClick={() => markAsRead(n.id)}
                    >
                      <DoneIcon />
                    </IconButton>
                  </Tooltip>
                )}
              </ListItem>
            ))}
          </List>

          {/* Pagination Controls */}
          <TablePagination
            component="div"
            count={total}
            page={page}
            onPageChange={(e, newPage) => setPage(newPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(e) => {
              setPageSize(parseInt(e.target.value, 10));
              setPage(0);
            }}
            rowsPerPageOptions={[5, 10, 20, 50]}
            labelDisplayedRows={({ from, to }) => `${from}–${to}`}
            nextIconButtonProps={{
              disabled:
                items.length < pageSize || (page + 1) * pageSize >= total,
            }}
            backIconButtonProps={{
              disabled: page === 0,
            }}
          />
        </>
      )}
    </Box>
  );
}

export default Notifications;
