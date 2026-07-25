import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { ticketsService } from '../../services/endpoints';
import type { SupportTicket, TicketReply } from '../../types';

interface TicketsState {
  tickets: SupportTicket[];
  currentTicket: SupportTicket | null;
  loading: boolean;
  actionLoading: boolean;
  error: string | null;
}

const initialState: TicketsState = {
  tickets: [],
  currentTicket: null,
  loading: false,
  actionLoading: false,
  error: null,
};

export const fetchMyTickets = createAsyncThunk(
  'tickets/fetchMyTickets',
  async (_, { rejectWithValue }) => {
    try {
      return await ticketsService.getMyTickets();
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const fetchAllTickets = createAsyncThunk(
  'tickets/fetchAll',
  async (_, { rejectWithValue }) => {
    try {
      return await ticketsService.getAll();
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const fetchTicketById = createAsyncThunk(
  'tickets/fetchById',
  async (ticketId: string, { rejectWithValue }) => {
    try {
      return await ticketsService.getById(ticketId);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const createTicket = createAsyncThunk(
  'tickets/create',
  async (
    data: { subject: string; category: string; description: string; requestId?: string },
    { rejectWithValue }
  ) => {
    try {
      return await ticketsService.create(data);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const addTicketReply = createAsyncThunk(
  'tickets/addReply',
  async (
    { ticketId, messageBody }: { ticketId: string; messageBody: string },
    { rejectWithValue }
  ) => {
    try {
      return await ticketsService.addReply(ticketId, { message: messageBody });
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const closeTicket = createAsyncThunk(
  'tickets/close',
  async (
    { ticketId, satisfactionRating }: { ticketId: string; satisfactionRating?: number },
    { rejectWithValue }
  ) => {
    try {
      await ticketsService.closeTicket(ticketId, satisfactionRating);
      return ticketId;
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const reopenTicket = createAsyncThunk(
  'tickets/reopen',
  async (ticketId: string, { rejectWithValue }) => {
    try {
      await ticketsService.reopenTicket(ticketId);
      return ticketId;
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const resolveTicket = createAsyncThunk(
  'tickets/resolve',
  async (ticketId: string, { rejectWithValue }) => {
    try {
      await ticketsService.resolveTicket(ticketId);
      return ticketId;
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const fetchQueueTickets = createAsyncThunk(
  'tickets/fetchQueue',
  async (_, { rejectWithValue }) => {
    try {
      return await ticketsService.getQueueTickets();
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const assignTicketToUser = createAsyncThunk(
  'tickets/assignToUser',
  async (
    { ticketId, userId }: { ticketId: string; userId: string },
    { rejectWithValue }
  ) => {
    try {
      return await ticketsService.assignToUser(ticketId, userId);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

const ticketsSlice = createSlice({
  name: 'tickets',
  initialState,
  reducers: {
    clearCurrentTicket(state) { state.currentTicket = null; },
    clearError(state) { state.error = null; },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchMyTickets.pending, (state) => { state.loading = true; })
      .addCase(fetchMyTickets.fulfilled, (state, action) => {
        state.loading = false;
        state.tickets = action.payload;
      })
      .addCase(fetchMyTickets.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(fetchAllTickets.fulfilled, (state, action) => {
        state.tickets = action.payload;
      });

    builder
      .addCase(fetchTicketById.pending, (state) => { state.loading = true; })
      .addCase(fetchTicketById.fulfilled, (state, action) => {
        state.loading = false;
        state.currentTicket = action.payload;
      })
      .addCase(fetchTicketById.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(createTicket.pending, (state) => { state.actionLoading = true; })
      .addCase(createTicket.fulfilled, (state, action) => {
        state.actionLoading = false;
        state.tickets.unshift(action.payload);
      })
      .addCase(createTicket.rejected, (state, action) => {
        state.actionLoading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(addTicketReply.pending, (state) => { state.actionLoading = true; })
      .addCase(addTicketReply.fulfilled, (state, action) => {
        state.actionLoading = false;
        if (state.currentTicket) {
          state.currentTicket.replies = [
            ...(state.currentTicket.replies || []),
            action.payload as TicketReply,
          ];
        }
      })
      .addCase(addTicketReply.rejected, (state, action) => {
        state.actionLoading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(closeTicket.fulfilled, (state, action) => {
        const t = state.tickets.find((t) => t.ticketId === action.payload);
        if (t) t.status = 'Closed';
        if (state.currentTicket?.ticketId === action.payload) {
          state.currentTicket.status = 'Closed';
        }
      });

    builder
      .addCase(reopenTicket.fulfilled, (state, action) => {
        const t = state.tickets.find((t) => t.ticketId === action.payload);
        if (t) t.status = 'Open';
        if (state.currentTicket?.ticketId === action.payload) {
          state.currentTicket.status = 'Open';
        }
      });

    builder
      .addCase(resolveTicket.fulfilled, (state, action) => {
        const t = state.tickets.find((t) => t.ticketId === action.payload);
        if (t) t.status = 'Resolved';
        if (state.currentTicket?.ticketId === action.payload) {
          state.currentTicket.status = 'Resolved';
        }
      });

    builder
      .addCase(fetchQueueTickets.pending, (state) => { state.loading = true; })
      .addCase(fetchQueueTickets.fulfilled, (state, action) => {
        state.loading = false;
        state.tickets = action.payload;
      })
      .addCase(fetchQueueTickets.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(assignTicketToUser.fulfilled, (state, action) => {
        state.actionLoading = false;
        if (state.currentTicket) {
          state.currentTicket = { ...state.currentTicket, ...action.payload };
        }
      });
  },
});

export const { clearCurrentTicket, clearError } = ticketsSlice.actions;
export default ticketsSlice.reducer;
