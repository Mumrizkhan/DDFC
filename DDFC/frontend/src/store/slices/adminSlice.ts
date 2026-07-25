import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { adminService } from '../../services/endpoints';
import type { AdminDashboardStats, User, Department, Role } from '../../types';

interface AdminState {
  dashboard: AdminDashboardStats | null;
  users: User[];
  departments: Department[];
  roles: Role[];
  loading: boolean;
  actionLoading: boolean;
  error: string | null;
}

const initialState: AdminState = {
  dashboard: null,
  users: [],
  departments: [],
  roles: [],
  loading: false,
  actionLoading: false,
  error: null,
};

export const fetchAdminDashboard = createAsyncThunk(
  'admin/fetchDashboard',
  async (_, { rejectWithValue }) => {
    try {
      return await adminService.getDashboard();
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const fetchUsers = createAsyncThunk(
  'admin/fetchUsers',
  async (_: undefined, { rejectWithValue }) => {
    try {
      return await adminService.getUsers();
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const createUser = createAsyncThunk(
  'admin/createUser',
  async (
    data: { fullName: string; email: string; password: string; roleId: string; departmentId?: string; isActive?: boolean },
    { rejectWithValue }
  ) => {
    try {
      return await adminService.createUser(data);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const updateUser = createAsyncThunk(
  'admin/updateUser',
  async (
    { userId, data }: { userId: string; data: Partial<User> },
    { rejectWithValue }
  ) => {
    try {
      return await adminService.updateUser(userId, data);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const deleteUser = createAsyncThunk(
  'admin/deleteUser',
  async (userId: string, { rejectWithValue }) => {
    try {
      await adminService.deleteUser(userId);
      return userId;
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const resetPassword = createAsyncThunk(
  'admin/resetPassword',
  async ({ userId, newPassword }: { userId: string; newPassword: string }, { rejectWithValue }) => {
    try {
      return await adminService.updateUser(userId, { newPassword } as Partial<User>);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const fetchDepartments = createAsyncThunk(
  'admin/fetchDepartments',
  async (_, { rejectWithValue }) => {
    try {
      return await adminService.getDepartments();
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const createDepartment = createAsyncThunk(
  'admin/createDepartment',
  async (data: Partial<Department>, { rejectWithValue }) => {
    try {
      return await adminService.createDepartment({ name: data.departmentName!, code: data.departmentCode! });
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const updateDepartment = createAsyncThunk(
  'admin/updateDepartment',
  async (
    { deptId, data }: { deptId: string; data: Partial<Department> },
    { rejectWithValue }
  ) => {
    try {
      return await adminService.updateDepartment(deptId, { name: data.departmentName, code: data.departmentCode });
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const fetchRoles = createAsyncThunk(
  'admin/fetchRoles',
  async (_, { rejectWithValue }) => {
    try {
      return await adminService.getRoles();
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const updateRolePermissions = createAsyncThunk(
  'admin/updateRolePermissions',
  async (
    { roleId, permissions }: { roleId: string; permissions: string },
    { rejectWithValue }
  ) => {
    try {
      return await adminService.updateRolePermissions(roleId, permissions);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

const adminSlice = createSlice({
  name: 'admin',
  initialState,
  reducers: {
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchAdminDashboard.pending, (state) => { state.loading = true; })
      .addCase(fetchAdminDashboard.fulfilled, (state, action) => {
        state.loading = false;
        state.dashboard = action.payload;
      })
      .addCase(fetchAdminDashboard.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(fetchUsers.pending, (state) => { state.loading = true; })
      .addCase(fetchUsers.fulfilled, (state, action) => {
        state.loading = false;
        state.users = action.payload as User[];
      })
      .addCase(fetchUsers.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(createUser.pending, (state) => { state.actionLoading = true; })
      .addCase(createUser.fulfilled, (state, action) => {
        state.actionLoading = false;
        state.users.unshift(action.payload);
      })
      .addCase(createUser.rejected, (state, action) => {
        state.actionLoading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(updateUser.fulfilled, (state, action) => {
        state.actionLoading = false;
        const idx = state.users.findIndex((u) => u.userId === action.payload.userId);
        if (idx !== -1) state.users[idx] = action.payload;
      });

    builder
      .addCase(deleteUser.fulfilled, (state, action) => {
        state.users = state.users.filter((u) => u.userId !== action.payload);
      });

    builder
      .addCase(fetchDepartments.fulfilled, (state, action) => {
        state.departments = action.payload;
      });

    builder
      .addCase(createDepartment.fulfilled, (state, action) => {
        state.departments.push(action.payload);
      });

    builder
      .addCase(fetchRoles.fulfilled, (state, action) => {
        state.roles = action.payload;
      });

    builder
      .addCase(updateDepartment.fulfilled, (state, action) => {
        const idx = state.departments.findIndex((d) => d.departmentId === action.payload.departmentId);
        if (idx !== -1) state.departments[idx] = action.payload;
      });
  },
});

export const { clearError } = adminSlice.actions;
export default adminSlice.reducer;
