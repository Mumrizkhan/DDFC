import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { authService } from '../../services/endpoints';
import type {
  User,
  Customer,
  StaffLoginRequest,
  CustomerOtpRequest,
  CustomerVerifyOtpRequest,
} from '../../types';

interface AuthState {
  staffUser: User | null;
  customer: Customer | null;
  staffToken: string | null;
  customerToken: string | null;
  loading: boolean;
  error: string | null;
  otpSent: boolean;
}

const initialState: AuthState = {
  staffUser: null,
  customer: null,
  staffToken: localStorage.getItem('staff_token'),
  customerToken: localStorage.getItem('customer_token'),
  loading: false,
  error: null,
  otpSent: false,
};

// Parse stored user if token exists
try {
  const stored = localStorage.getItem('staff_user');
  if (stored) initialState.staffUser = JSON.parse(stored);
} catch {
  // ignore
}
try {
  const stored = localStorage.getItem('customer_user');
  if (stored) initialState.customer = JSON.parse(stored);
} catch {
  // ignore
}

// ── Thunks ──────────────────────────────────────────────────────────────────

export const staffLogin = createAsyncThunk(
  'auth/staffLogin',
  async (data: StaffLoginRequest, { rejectWithValue }) => {
    try {
      return await authService.staffLogin(data);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const customerRequestOtp = createAsyncThunk(
  'auth/customerRequestOtp',
  async (data: CustomerOtpRequest, { rejectWithValue }) => {
    try {
      return await authService.customerRequestOtp(data);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const customerVerifyOtp = createAsyncThunk(
  'auth/customerVerifyOtp',
  async (data: CustomerVerifyOtpRequest, { rejectWithValue }) => {
    try {
      return await authService.customerVerifyOtp(data);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

// ── Slice ────────────────────────────────────────────────────────────────────

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    staffLogout(state) {
      state.staffUser = null;
      state.staffToken = null;
      localStorage.removeItem('staff_token');
      localStorage.removeItem('staff_user');
    },
    customerLogout(state) {
      state.customer = null;
      state.customerToken = null;
      localStorage.removeItem('customer_token');
      localStorage.removeItem('customer_user');
    },
    clearError(state) {
      state.error = null;
    },
    resetOtpSent(state) {
      state.otpSent = false;
    },
  },
  extraReducers: (builder) => {
    // Staff login
    builder
      .addCase(staffLogin.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(staffLogin.fulfilled, (state, action) => {
        state.loading = false;
        state.staffToken = action.payload.token;
        state.staffUser = action.payload.user;
        localStorage.setItem('staff_token', action.payload.token);
        localStorage.setItem(
          'staff_user',
          JSON.stringify(action.payload.user)
        );
      })
      .addCase(staffLogin.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    // Customer OTP request
    builder
      .addCase(customerRequestOtp.pending, (state) => {
        state.loading = true;
        state.error = null;
        state.otpSent = false;
      })
      .addCase(customerRequestOtp.fulfilled, (state) => {
        state.loading = false;
        state.otpSent = true;
      })
      .addCase(customerRequestOtp.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    // Customer OTP verify
    builder
      .addCase(customerVerifyOtp.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(customerVerifyOtp.fulfilled, (state, action) => {
        state.loading = false;
        state.customerToken = action.payload.token;
        state.customer = action.payload.customer;
        localStorage.setItem('customer_token', action.payload.token);
        localStorage.setItem(
          'customer_user',
          JSON.stringify(action.payload.customer)
        );
      })
      .addCase(customerVerifyOtp.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });
  },
});

export const { staffLogout, customerLogout, clearError, resetOtpSent } =
  authSlice.actions;
export default authSlice.reducer;
