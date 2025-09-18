import { createContext, useState, useEffect, useContext } from "react";
import api from "../api/axiosClient";
import { AuthContext } from "./AuthContext";

export const NotificationContext = createContext();

export const NotificationProvider = ({ children }) => {
  const { user } = useContext(AuthContext); // <-- depend on auth user
  const [unreadCount, setUnreadCount] = useState(0);

  const loadUnread = async () => {
    if (!user) {
      // no user logged in
      setUnreadCount(0);
      return;
    }
    try {
      const { data } = await api.get("/employee/notifications/unread-count");
      setUnreadCount(data.count);
    } catch {
      setUnreadCount(0);
    }
  };

  useEffect(() => {
    // whenever user changes, reload unread count
    loadUnread();

    // optional: keep polling while logged in
    let interval;
    if (user) {
      interval = setInterval(loadUnread, 60000);
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [user]); // <-- key: depend on user

  return (
    <NotificationContext.Provider value={{ unreadCount, loadUnread }}>
      {children}
    </NotificationContext.Provider>
  );
};
