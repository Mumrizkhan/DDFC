import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { packagesService } from '../../services/endpoints';
import type { Package } from '../../types';

interface PackagesState {
  packages: Package[];
  loading: boolean;
  error: string | null;
}

const initialState: PackagesState = {
  packages: [],
  loading: false,
  error: null,
};

export const fetchPackages = createAsyncThunk(
  'packages/fetchAll',
  async (
    params: { plotType?: string; plotSize?: string; category?: string; designType?: string } | undefined,
    { rejectWithValue }
  ) => {
    try {
      return await packagesService.getAll(params);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const updatePackage = createAsyncThunk(
  'packages/update',
  async (
    { id, data }: { id: string; data: Partial<Package> },
    { rejectWithValue }
  ) => {
    try {
      return await packagesService.update(id, data.lineItems ?? []);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

const packagesSlice = createSlice({
  name: 'packages',
  initialState,
  reducers: {
    clearError(state) { state.error = null; },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchPackages.pending, (state) => { state.loading = true; })
      .addCase(fetchPackages.fulfilled, (state, action) => {
        state.loading = false;
        state.packages = action.payload;
      })
      .addCase(fetchPackages.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(updatePackage.fulfilled, (state, action) => {
        const idx = state.packages.findIndex(
          (p) => p.packageId === action.payload.packageId
        );
        if (idx !== -1) state.packages[idx] = action.payload;
      });
  },
});

export const { clearError } = packagesSlice.actions;
export default packagesSlice.reducer;
