import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { tasksService } from '../../services/endpoints';
import type { TaskAssignment, EmployeeWorkload } from '../../types';

interface TasksState {
  myTasks: TaskAssignment[];
  tasksForRequest: TaskAssignment[];
  departmentTasks: TaskAssignment[];
  unassignedTasks: TaskAssignment[];
  workload: EmployeeWorkload[];
  loading: boolean;
  actionLoading: boolean;
  error: string | null;
}

const initialState: TasksState = {
  myTasks: [],
  tasksForRequest: [],
  departmentTasks: [],
  unassignedTasks: [],
  workload: [],
  loading: false,
  actionLoading: false,
  error: null,
};

export const fetchMyTasks = createAsyncThunk(
  'tasks/fetchMyTasks',
  async (_, { rejectWithValue }) => {
    try { return await tasksService.getMyTasks(); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const fetchTasksForRequest = createAsyncThunk(
  'tasks/fetchTasksForRequest',
  async (requestId: string, { rejectWithValue }) => {
    try { return await tasksService.getForRequest(requestId); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const fetchDepartmentTasks = createAsyncThunk(
  'tasks/fetchDepartmentTasks',
  async (deptId: string, { rejectWithValue }) => {
    try { return await tasksService.getDepartmentTasks(deptId); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const fetchUnassignedTasks = createAsyncThunk(
  'tasks/fetchUnassignedTasks',
  async (deptId: string, { rejectWithValue }) => {
    try { return await tasksService.getUnassignedTasks(deptId); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const fetchDepartmentWorkload = createAsyncThunk(
  'tasks/fetchDepartmentWorkload',
  async (deptId: string, { rejectWithValue }) => {
    try { return await tasksService.getDepartmentWorkload(deptId); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const assignTask = createAsyncThunk(
  'tasks/assignTask',
  async ({ taskId, employeeId, reason }: { taskId: string; employeeId: string; reason?: string }, { rejectWithValue }) => {
    try { return await tasksService.assignTask(taskId, { employeeId, reason }); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const reassignTask = createAsyncThunk(
  'tasks/reassignTask',
  async ({ taskId, newUserId, reason }: { taskId: string; newUserId: string; reason: string }, { rejectWithValue }) => {
    try { return await tasksService.reassignTask(taskId, { newUserId, reason }); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const completeTask = createAsyncThunk(
  'tasks/completeTask',
  async (taskId: string, { rejectWithValue }) => {
    try { return await tasksService.completeTask(taskId); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const setEmployeeAvailability = createAsyncThunk(
  'tasks/setEmployeeAvailability',
  async ({ userId, isAvailable, reason, unavailableUntil }: { userId: string; isAvailable: boolean; reason?: string; unavailableUntil?: string }, { rejectWithValue }) => {
    try { return await tasksService.setEmployeeAvailability(userId, { isAvailable, reason, unavailableUntil }); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const resetRoundRobin = createAsyncThunk(
  'tasks/resetRoundRobin',
  async (deptId: string, { rejectWithValue }) => {
    try { return await tasksService.resetRoundRobin(deptId); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

const tasksSlice = createSlice({
  name: 'tasks',
  initialState,
  reducers: {
    clearError(state) { state.error = null; },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchMyTasks.pending, (state) => { state.loading = true; })
      .addCase(fetchMyTasks.fulfilled, (state, action) => { state.loading = false; state.myTasks = action.payload; })
      .addCase(fetchMyTasks.rejected, (state, action) => { state.loading = false; state.error = action.payload as string; });

    builder
      .addCase(fetchTasksForRequest.pending, (state) => { state.loading = true; })
      .addCase(fetchTasksForRequest.fulfilled, (state, action) => { state.loading = false; state.tasksForRequest = action.payload; })
      .addCase(fetchTasksForRequest.rejected, (state, action) => { state.loading = false; state.error = action.payload as string; });

    builder
      .addCase(fetchDepartmentTasks.pending, (state) => { state.loading = true; })
      .addCase(fetchDepartmentTasks.fulfilled, (state, action) => { state.loading = false; state.departmentTasks = action.payload; })
      .addCase(fetchDepartmentTasks.rejected, (state, action) => { state.loading = false; state.error = action.payload as string; });

    builder
      .addCase(fetchUnassignedTasks.fulfilled, (state, action) => { state.unassignedTasks = action.payload; });

    builder
      .addCase(fetchDepartmentWorkload.fulfilled, (state, action) => { state.workload = action.payload; });

    builder
      .addCase(assignTask.pending, (state) => { state.actionLoading = true; })
      .addCase(assignTask.fulfilled, (state) => { state.actionLoading = false; })
      .addCase(assignTask.rejected, (state, action) => { state.actionLoading = false; state.error = action.payload as string; });

    builder
      .addCase(reassignTask.pending, (state) => { state.actionLoading = true; })
      .addCase(reassignTask.fulfilled, (state) => { state.actionLoading = false; })
      .addCase(reassignTask.rejected, (state, action) => { state.actionLoading = false; state.error = action.payload as string; });

    builder
      .addCase(completeTask.pending, (state) => { state.actionLoading = true; })
      .addCase(completeTask.fulfilled, (state) => { state.actionLoading = false; })
      .addCase(completeTask.rejected, (state, action) => { state.actionLoading = false; state.error = action.payload as string; });

    builder
      .addCase(setEmployeeAvailability.pending, (state) => { state.actionLoading = true; })
      .addCase(setEmployeeAvailability.fulfilled, (state) => { state.actionLoading = false; })
      .addCase(setEmployeeAvailability.rejected, (state, action) => { state.actionLoading = false; state.error = action.payload as string; });
  },
});

export const { clearError } = tasksSlice.actions;
export default tasksSlice.reducer;
