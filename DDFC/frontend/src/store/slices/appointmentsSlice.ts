import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { appointmentsService } from '../../services/endpoints';
import type { Appointment } from '../../types';

interface AppointmentsState {
  appointments: Appointment[];
  loading: boolean;
  error: string | null;
}

const initialState: AppointmentsState = {
  appointments: [],
  loading: false,
  error: null,
};

export const fetchAppointments = createAsyncThunk(
  'appointments/fetchAll',
  async (params: { departmentId?: string; status?: string; date?: string } | undefined, { rejectWithValue }) => {
    try { return await appointmentsService.getAll(params); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const bookAppointment = createAsyncThunk(
  'appointments/book',
  async (data: Parameters<typeof appointmentsService.book>[0], { rejectWithValue }) => {
    try { return await appointmentsService.book(data); }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const completeAppointment = createAsyncThunk(
  'appointments/complete',
  async ({ id, notes }: { id: string; notes?: string }, { rejectWithValue }) => {
    try { await appointmentsService.complete(id, notes); return id; }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const cancelAppointment = createAsyncThunk(
  'appointments/cancel',
  async ({ id, notes }: { id: string; notes?: string }, { rejectWithValue }) => {
    try { await appointmentsService.cancel(id, notes); return id; }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

export const noShowAppointment = createAsyncThunk(
  'appointments/noShow',
  async (id: string, { rejectWithValue }) => {
    try { await appointmentsService.noShow(id); return id; }
    catch (err) { return rejectWithValue((err as { message: string }).message); }
  }
);

const appointmentsSlice = createSlice({
  name: 'appointments',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchAppointments.pending, (s) => { s.loading = true; s.error = null; })
      .addCase(fetchAppointments.fulfilled, (s, a) => { s.loading = false; s.appointments = a.payload; })
      .addCase(fetchAppointments.rejected, (s, a) => { s.loading = false; s.error = a.payload as string; });

    builder.addCase(bookAppointment.fulfilled, (s, a) => {
      s.appointments.unshift(a.payload);
    });

    const patchStatus = (id: string, status: Appointment['status'], s: AppointmentsState) => {
      const a = s.appointments.find((x) => x.id === id);
      if (a) a.status = status;
    };

    builder.addCase(completeAppointment.fulfilled, (s, a) => patchStatus(a.payload, 'Completed', s));
    builder.addCase(cancelAppointment.fulfilled,   (s, a) => patchStatus(a.payload, 'Cancelled', s));
    builder.addCase(noShowAppointment.fulfilled,   (s, a) => patchStatus(a.payload, 'NoShow', s));
  },
});

export default appointmentsSlice.reducer;
