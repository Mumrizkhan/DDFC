import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { requestsService } from '../../services/endpoints';
import type { PossessionRequest, CreateRequestDto } from '../../types';

interface RequestsState {
  requests: PossessionRequest[];
  currentRequest: PossessionRequest | null;
  totalCount: number;
  loading: boolean;
  actionLoading: boolean;
  error: string | null;
}

const initialState: RequestsState = {
  requests: [],
  currentRequest: null,
  totalCount: 0,
  loading: false,
  actionLoading: false,
  error: null,
};

export const fetchRequests = createAsyncThunk(
  'requests/fetchAll',
  async (
    params: { status?: string | string[]; page?: number; pageSize?: number } | undefined,
    { rejectWithValue }
  ) => {
    try {
      return await requestsService.getAll(params);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const fetchRequestById = createAsyncThunk(
  'requests/fetchById',
  async (id: string, { rejectWithValue }) => {
    try {
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const fetchMyRequests = createAsyncThunk(
  'requests/fetchMyRequests',
  async (_, { rejectWithValue }) => {
    try {
      return await requestsService.getMyRequests();
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const createRequest = createAsyncThunk(
  'requests/create',
  async (data: CreateRequestDto, { rejectWithValue }) => {
    try {
      return await requestsService.create(data);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const transferApprove = createAsyncThunk(
  'requests/transferApprove',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.transferApprove(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const transferReject = createAsyncThunk(
  'requests/transferReject',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.transferReject(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const transferClarification = createAsyncThunk(
  'requests/transferClarification',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.transferClarification(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const financeApprove = createAsyncThunk(
  'requests/financeApprove',
  async ({ id, comments, adcAmount }: { id: string; comments?: string; adcAmount?: number }, { rejectWithValue }) => {
    try {
      await requestsService.financeApprove(id, { comments, adcAmount });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const financeReject = createAsyncThunk(
  'requests/financeReject',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.financeReject(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const approvePlan = createAsyncThunk(
  'requests/approvePlan',
  async ({ requestId, planId }: { requestId: string; planId: string }, { rejectWithValue }) => {
    try {
      await requestsService.approvePlan(requestId, planId);
      return await requestsService.getById(requestId);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const requestPlanRevision = createAsyncThunk(
  'requests/requestPlanRevision',
  async (
    { requestId, planId, comments, markupFileUrl }: { requestId: string; planId: string; comments: string; markupFileUrl?: string },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.requestPlanRevision(requestId, planId, { comments, markupFileUrl });
      return await requestsService.getById(requestId);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const ddfcAdminSign = createAsyncThunk(
  'requests/ddfcAdminSign',
  async (payload: {
    id: string;
    handedOverBy?: string;
    handedOverDate?: string;
    takenOverBy?: string;
    takenOverDate?: string;
    chiefSurveyorName?: string;
    adTpBcdName?: string;
  }, { rejectWithValue }) => {
    try {
      const { id, ...certData } = payload;
      await requestsService.ddfcAdminSign(id, certData);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const requestDelayUndertaking = createAsyncThunk(
  'requests/requestDelayUndertaking',
  async (
    { id, data }: { id: string; data: { delayReason?: string; expectedDelayDays?: number; notes?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.requestDelayUndertaking(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const signDelayUndertaking = createAsyncThunk(
  'requests/signDelayUndertaking',
  async (
    { id, data }: { id: string; data: { undertakingDocumentUrl?: string; notes?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.signDelayUndertaking(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const skipDelayUndertaking = createAsyncThunk(
  'requests/skipDelayUndertaking',
  async (id: string, { rejectWithValue }) => {
    try {
      await requestsService.skipDelayUndertaking(id);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const attachSignedUndertaking = createAsyncThunk(
  'requests/attachSignedUndertaking',
  async (
    { id, data }: { id: string; data: { signedDocumentUrl: string; holdDays: number; notes?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.attachSignedUndertaking(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const requestAnnexation = createAsyncThunk(
  'requests/requestAnnexation',
  async (
    { id, data }: { id: string; data: { additionalArea?: string; annexationFee: number; notes?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.requestAnnexation(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const requestPlotMerge = createAsyncThunk(
  'requests/requestPlotMerge',
  async (
    { id, data }: { id: string; data: { mergedPlotNumber: string; mergedPlotSector: string; mergedPlotSize?: string; notes?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.requestPlotMerge(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const assignCadOperator = createAsyncThunk(
  'requests/assignCadOperator',
  async (
    { id, data }: { id: string; data: { cadType: string; assignedUserId: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.assignCadOperator(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const submitDeptCadFile = createAsyncThunk(
  'requests/submitDeptCadFile',
  async (
    { id, data }: { id: string; data: { cadType: string; fileUrl: string; fileName?: string; fileType?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.submitDeptCadFile(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const selectPackage = createAsyncThunk(
  'requests/selectPackage',
  async (
    { id, packageId, interiorDesignPackageId, supervisionPackageId }: {
      id: string;
      packageId: string;
      interiorDesignPackageId?: string;
      supervisionPackageId?: string;
    },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.selectPackage(id, packageId, interiorDesignPackageId, supervisionPackageId);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const confirmPayment = createAsyncThunk(
  'requests/confirmPayment',
  async (
    { id, data }: { id: string; data: { challanNo?: string; amountPaid: number; scannedChallanFileUrl?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.confirmPayment(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const uploadPlan = createAsyncThunk(
  'requests/uploadPlan',
  async (
    { id, data }: { id: string; data: { fileUrl: string; notes?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.uploadPlan(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const threeDUploadFile = createAsyncThunk(
  'requests/threeDUploadFile',
  async (
    { id, data }: { id: string; data: { fileUrl: string; fileName: string; fileType: string; notes?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.threeDUploadFile(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const threeDComplete = createAsyncThunk(
  'requests/threeDComplete',
  async (id: string, { rejectWithValue }) => {
    try {
      await requestsService.threeDComplete(id);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const cadUploadFile = createAsyncThunk(
  'requests/cadUploadFile',
  async (
    { id, data }: { id: string; data: { fileUrl: string; fileName: string; fileType: string; notes?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.cadUploadFile(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const cadComplete = createAsyncThunk(
  'requests/cadComplete',
  async (id: string, { rejectWithValue }) => {
    try {
      await requestsService.cadComplete(id);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const structureComplete = createAsyncThunk(
  'requests/structureComplete',
  async (
    { id, data }: { id: string; data: { fileUrl: string; observations?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.structureComplete(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const mepComplete = createAsyncThunk(
  'requests/mepComplete',
  async (
    { id, data }: { id: string; data: { fileUrl: string; observations?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.mepComplete(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const principalApprove = createAsyncThunk(
  'requests/principalApprove',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.principalApprove(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const paInitialReview = createAsyncThunk(
  'requests/paInitialReview',
  async (
    { id, data }: { id: string; data: {
      assignedArchitectId: string;
      soilTestFileUrl?: string;
      soilTestDate?: string;
      labName?: string;
      soilBearingCapacity?: string;
      resultSummary?: string;
      notes?: string;
    }},
    { rejectWithValue }
  ) => {
    try {
      return await requestsService.paInitialReview(id, data);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const principalSendBack = createAsyncThunk(
  'requests/principalSendBack',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.principalSendBack(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const uploadSoilTest = createAsyncThunk(
  'requests/uploadSoilTest',
  async (
    { id, data }: { id: string; data: { testDate: string; labName: string; soilBearingCapacity: number; resultSummary: string; reportFileUrl: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.uploadSoilTest(id, {
        testDate:            data.testDate,
        labName:             data.labName,
        soilBearingCapacity: String(data.soilBearingCapacity),
        resultSummary:       data.resultSummary,
        reportFileUrl:       data.reportFileUrl,
      });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const submitBuildingControl = createAsyncThunk(
  'requests/submitBuildingControl',
  async (
    { id, data }: { id: string; data: { surveyDate: string; officerName: string; observations?: string; violations?: string; suggestions?: string; photoUrlsJson?: string } },
    { rejectWithValue }
  ) => {
    try {
      await requestsService.submitBuildingControl(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const finalApprove = createAsyncThunk(
  'requests/finalApprove',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.finalApprove(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const finalReject = createAsyncThunk(
  'requests/finalReject',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.finalReject(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const deliverRequest = createAsyncThunk(
  'requests/deliver',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.deliver(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const rejectRequest = createAsyncThunk(
  'requests/rejectRequest',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.rejectRequest(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const adminReview = createAsyncThunk(
  'requests/adminReview',
  async ({ id, data }: {
    id: string;
    data: {
      action: string;
      rejectionReason?: string;
      allotmentLetterUrl?: string;
      cnicUrl?: string;
      messageScreenshotUrl?: string;
      eStampPaperUrl?: string;
      authorizedPersonCnicUrl?: string;
      authorizedPersonPhone?: string;
    };
  }, { rejectWithValue }) => {
    try {
      await requestsService.adminReview(id, data);
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

export const initiateRequest = createAsyncThunk(
  'requests/initiateRequest',
  async ({ id, comments }: { id: string; comments?: string }, { rejectWithValue }) => {
    try {
      await requestsService.initiateRequest(id, { comments });
      return await requestsService.getById(id);
    } catch (err) {
      return rejectWithValue((err as { message: string }).message);
    }
  }
);

const requestsSlice = createSlice({
  name: 'requests',
  initialState,
  reducers: {
    clearCurrentRequest(state) {
      state.currentRequest = null;
    },
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    const handlePending = (state: RequestsState) => {
      state.loading = true;
      state.error = null;
    };
    const handleRejected = (state: RequestsState, action: { payload: unknown }) => {
      state.loading = false;
      state.error = action.payload as string;
    };

    builder
      .addCase(fetchRequests.pending, handlePending)
      .addCase(fetchRequests.fulfilled, (state, action) => {
        state.loading = false;
        state.requests = action.payload as PossessionRequest[];
        state.totalCount = state.requests.length;
      })
      .addCase(fetchRequests.rejected, handleRejected);

    builder
      .addCase(fetchRequestById.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchRequestById.fulfilled, (state, action) => {
        state.loading = false;
        state.currentRequest = action.payload;
      })
      .addCase(fetchRequestById.rejected, handleRejected);

    builder
      .addCase(fetchMyRequests.pending, handlePending)
      .addCase(fetchMyRequests.fulfilled, (state, action) => {
        state.loading = false;
        state.requests = action.payload;
      })
      .addCase(fetchMyRequests.rejected, handleRejected);

    builder
      .addCase(createRequest.pending, (state) => {
        state.actionLoading = true;
        state.error = null;
      })
      .addCase(createRequest.fulfilled, (state, action) => {
        state.actionLoading = false;
        state.requests.unshift(action.payload);
      })
      .addCase(createRequest.rejected, (state, action) => {
        state.actionLoading = false;
        state.error = action.payload as string;
      });

    // Workflow actions
    const workflowCases = [
      transferApprove, transferReject, transferClarification,
      financeApprove, financeReject, approvePlan, requestPlanRevision,
      ddfcAdminSign, requestDelayUndertaking, signDelayUndertaking, skipDelayUndertaking,
      attachSignedUndertaking, requestAnnexation, requestPlotMerge,
      assignCadOperator, submitDeptCadFile,
      selectPackage, confirmPayment, uploadPlan,
      threeDUploadFile, threeDComplete,
      cadUploadFile, cadComplete,
      structureComplete, mepComplete, principalApprove, principalSendBack, paInitialReview,
      uploadSoilTest, submitBuildingControl,
      finalApprove, finalReject, deliverRequest, rejectRequest, initiateRequest, adminReview,
    ];
    workflowCases.forEach((thunk) => {
      builder
        .addCase(thunk.pending, (state) => {
          state.actionLoading = true;
          state.error = null;
        })
        .addCase(thunk.fulfilled, (state, action) => {
          state.actionLoading = false;
          state.currentRequest = action.payload as PossessionRequest;
        })
        .addCase(thunk.rejected, (state, action) => {
          state.actionLoading = false;
          state.error = action.payload as string;
        });
    });
  },
});

export const { clearCurrentRequest, clearError } = requestsSlice.actions;
export default requestsSlice.reducer;
