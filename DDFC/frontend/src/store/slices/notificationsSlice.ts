import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { notificationsService } from '../../services/endpoints';
import type { CustomerNotification } from '../../types';

interface NotificationsState {
  notifications: CustomerNotification[];
  unreadCount: number;
  loading: boolean;
  error: string | null;
}

const initialState: NotificationsState = {
  notifications: [],
  unreadCount: 0,
  loading: false,
  error: null,
};

export const fetchNotifications = createAsyncThunk(
  'notifications/fetchAll',
  async (unreadOnly: boolean | undefined, { rejectWithValue }) => {
    try {
      return await notificationsService.getAll(unreadOnly);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const markNotificationRead = createAsyncThunk(
  'notifications/markRead',
  async (notifId: string, { rejectWithValue }) => {
    try {
      await notificationsService.markRead(notifId);
      return notifId;
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const respondToNotification = createAsyncThunk(
  'notifications/respond',
  async (
    { notifId, responseText }: { notifId: string; responseText: string },
    { rejectWithValue }
  ) => {
    try {
      await notificationsService.respond(notifId, responseText);
      return notifId;
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

const notificationsSlice = createSlice({
  name: 'notifications',
  initialState,
  reducers: {
    markAllRead(state) {
      state.notifications.forEach((n) => { n.isRead = true; });
      state.unreadCount = 0;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchNotifications.pending, (state) => { state.loading = true; })
      .addCase(fetchNotifications.fulfilled, (state, action) => {
        state.loading = false;
        state.notifications = action.payload;
        state.unreadCount = action.payload.filter((n) => !n.isRead).length;
      })
      .addCase(fetchNotifications.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(markNotificationRead.fulfilled, (state, action) => {
        const n = state.notifications.find((n) => n.notificationId === action.payload);
        if (n && !n.isRead) {
          n.isRead = true;
          state.unreadCount = Math.max(0, state.unreadCount - 1);
        }
      });
  },
});

export const { markAllRead } = notificationsSlice.actions;
export default notificationsSlice.reducer;
