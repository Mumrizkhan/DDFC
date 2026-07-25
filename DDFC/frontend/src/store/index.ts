import { configureStore } from '@reduxjs/toolkit';
import authReducer from './slices/authSlice';
import requestsReducer from './slices/requestsSlice';
import adminReducer from './slices/adminSlice';
import tasksReducer from './slices/tasksSlice';
import notificationsReducer from './slices/notificationsSlice';
import ticketsReducer from './slices/ticketsSlice';
import packagesReducer from './slices/packagesSlice';
import appointmentsReducer from './slices/appointmentsSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    requests: requestsReducer,
    admin: adminReducer,
    tasks: tasksReducer,
    notifications: notificationsReducer,
    tickets: ticketsReducer,
    packages: packagesReducer,
    appointments: appointmentsReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
