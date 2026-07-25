import api from './api';
import type {
  StaffLoginRequest,
  CustomerOtpRequest,
  CustomerVerifyOtpRequest,
  AuthResponse,
  CustomerAuthResponse,
  User,
  Customer,
  Department,
  Role,
  PossessionRequest,
  CreateRequestDto,
  Package,
  PackageLineItem,
  ArchitecturalPlan,
  TaskAssignment,
  EmployeeWorkload,
  CustomerNotification,
  SupportTicket,
  TicketReply,
  AdminDashboardStats,
  Template,
  SurveyObservation,
  Appointment,
  AvailableEmployee,
} from '../types';

// ─── Auth ────────────────────────────────────────────────────────────────────

export const authService = {
  // Backend now returns { token, user: { userId, fullName, email, roleName, roleId, departmentId, departmentName, permissions } }
  // Normalize to frontend User shape here so the rest of the app stays consistent. Always use 'permissions' (plural).
  staffLogin: async (data: StaffLoginRequest): Promise<AuthResponse> => {
    const r = await api.post<{
      token: string;
      user: {
        userId: string; fullName: string; email: string;
        roleName?: string; roleId?: string;
        departmentId?: string; departmentName?: string; permissions?: string;
      };
    }>('/auth/staff/login', data);
    const { token, user: u } = r.data;
    return {
      token,
      user: {
        userId:         String(u.userId),
        fullName:       u.fullName,
        email:          u.email,
        roleId:         u.roleId ? String(u.roleId) : '',
        roleName:       u.roleName ?? '',
        departmentId:   u.departmentId ? String(u.departmentId) : undefined,
        departmentName: u.departmentName,
        isActive:       true,
        permissions:    u.permissions ? (
          typeof u.permissions === 'string' 
            ? (u.permissions.startsWith('[') ? JSON.parse(u.permissions) : u.permissions)
            : u.permissions
        ) : [],
      } as User,
    };
  },

  customerRequestOtp: (data: CustomerOtpRequest) =>
    api.post('/auth/customer/request-otp', data).then((r) => r.data),

  // Backend returns { token, customerId, fullName } — normalize to CustomerAuthResponse shape
  customerVerifyOtp: async (data: CustomerVerifyOtpRequest): Promise<CustomerAuthResponse> => {
    const r = await api.post<{ token: string; customerId: string; fullName: string }>('/auth/customer/verify-otp', data);
    const { token, customerId, fullName } = r.data;
    return {
      token,
      customer: {
        customerId,
        fullName,
        cnic: data.cnic,
        phoneNumber: '',
        createdAt: new Date().toISOString(),
      } as Customer,
    };
  },
};

// ─── Admin ───────────────────────────────────────────────────────────────────

export const adminService = {
  getDashboard: () =>
    api.get<AdminDashboardStats>('/admin/dashboard').then((r) => r.data),

  getUsers: () =>
    api.get<User[]>('/admin/users').then((r) => r.data),

  getUser: (userId: string) =>
    api.get<User>(`/admin/users/${userId}`).then((r) => r.data),

  createUser: (data: { fullName: string; email: string; password: string; roleId: string; departmentId?: string }) =>
    api.post<User>('/admin/users', data).then((r) => r.data),

  updateUser: (userId: string, data: { fullName?: string; roleId?: string; departmentId?: string; isActive?: boolean; newPassword?: string }) =>
    api.put<User>(`/admin/users/${userId}`, data).then((r) => r.data),

  // Reset password: triggers PUT with a generated password via newPassword field
  resetPassword: (userId: string, newPassword: string) =>
    api.put(`/admin/users/${userId}`, { newPassword }).then((r) => r.data),

  deleteUser: (userId: string) =>
    api.delete(`/admin/users/${userId}`).then((r) => r.data),

  getCustomers: () =>
    api.get<Customer[]>('/admin/customers').then((r) => r.data),

  createCustomer: (data: { fullName: string; cnic: string; phoneNumber: string; email?: string }) =>
    api.post<Customer>('/admin/customers', data).then((r) => r.data),

  getDepartments: async (): Promise<Department[]> => {
    const r = await api.get<Array<{
      id: string; departmentName: string; departmentCode: string;
      headUserId?: string; users?: unknown[];
    }>>('/admin/departments');
    return r.data.map((d) => ({
      departmentId:   d.id,
      departmentName: d.departmentName,
      departmentCode: d.departmentCode,
      headUserId:     d.headUserId,
      employeeCount:  d.users?.length ?? 0,
    }));
  },

  createDepartment: async (data: { name: string; code: string }): Promise<Department> => {
    const r = await api.post<{ id: string; departmentName: string; departmentCode: string }>('/admin/departments', data);
    return { departmentId: r.data.id, departmentName: r.data.departmentName, departmentCode: r.data.departmentCode };
  },

  updateDepartment: async (deptId: string, data: { name?: string; code?: string }): Promise<Department> => {
    const r = await api.put<{ id: string; departmentName: string; departmentCode: string }>(`/admin/departments/${deptId}`, data);
    return { departmentId: r.data.id, departmentName: r.data.departmentName, departmentCode: r.data.departmentCode };
  },

  getRoles: async (): Promise<Role[]> => {
    const r = await api.get<Array<{ id: string; name: string; permissions: string | null; isBuiltIn?: boolean }>>('/admin/roles');
    return r.data.map((raw) => ({
      roleId:      raw.id,
      roleName:    raw.name,
      permissions: (() => {
        if (!raw.permissions) return {};
        try {
          const arr: string[] = JSON.parse(raw.permissions);
          return Object.fromEntries(arr.map((p) => [p, true]));
        } catch { return {}; }
      })(),
    }));
  },

  /** permissions is a JSON string of the permissions object */
  updateRolePermissions: (roleId: string, permissions: string) =>
    api.put(`/admin/roles/${roleId}/permissions`, { permissions }).then((r) => r.data),

  getPlots: (params?: { status?: string; sectorNo?: string; phaseNo?: string }) =>
    api.get('/admin/plots', { params }).then((r) => r.data),

  getPlotPhases: () =>
    api.get<string[]>('/admin/plots/phases').then((r) => r.data),

  getPlotSectors: (phaseNo?: string) =>
    api.get<string[]>('/admin/plots/sectors', { params: { phaseNo } }).then((r) => r.data),

  createPlot: (data: { plotNumber: string; sectorNo: string; streetNo?: string; phaseNo?: string; plotSize: string; plotType: string }) =>
    api.post('/admin/plots', data).then((r) => r.data),
};

// ─── Requests ────────────────────────────────────────────────────────────────

export const requestsService = {
  getAll: (params?: { status?: string | string[] }) =>
    api
      .get<PossessionRequest[]>('/requests', {
        params: {
          ...params,
          status: Array.isArray(params?.status)
            ? params.status.join(',')
            : params?.status,
        },
      })
      .then((r) => r.data),

  getById: (id: string) =>
    api.get<PossessionRequest>(`/requests/${id}`).then((r) => r.data),

  getMyRequests: () =>
    api.get<PossessionRequest[]>('/requests/my').then((r) => r.data),

  create: (data: CreateRequestDto) =>
    api.post<PossessionRequest>('/requests', data).then((r) => r.data),

  getHistory: (id: string) =>
    api.get(`/requests/${id}/history`).then((r) => r.data),

  // ── Workflow steps ──────────────────────────────────────────────────────────
  transferApprove: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/transfer/approve`, data).then((r) => r.data),
  transferReject: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/transfer/reject`, data).then((r) => r.data),
  transferClarification: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/transfer/clarification`, data).then((r) => r.data),

  financeApprove: (id: string, data: { comments?: string; adcAmount?: number }) =>
    api.post(`/requests/${id}/finance/approve`, data).then((r) => r.data),

  financeReject: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/finance/reject`, data).then((r) => r.data),

  ddfcAdminSign: (id: string, data: {
    handedOverBy?: string;
    handedOverDate?: string;
    takenOverBy?: string;
    takenOverDate?: string;
    chiefSurveyorName?: string;
    adTpBcdName?: string;
  }) =>
    api.post(`/requests/${id}/ddfc-admin/sign`, data).then((r) => r.data),

  // ── Delay Undertaking ───────────────────────────────────────────────────
  requestDelayUndertaking: (id: string, data: { delayReason?: string; expectedDelayDays?: number; notes?: string }) =>
    api.post(`/requests/${id}/delay-undertaking/request`, data).then((r) => r.data),

  signDelayUndertaking: (id: string, data: { undertakingDocumentUrl?: string; notes?: string }) =>
    api.post(`/requests/${id}/delay-undertaking/sign`, data).then((r) => r.data),

  skipDelayUndertaking: (id: string) =>
    api.post(`/requests/${id}/delay-undertaking/skip`).then((r) => r.data),

  // ── Architecture Stage Undertaking ─────────────────────────────────────
  /** Returns HTML string for preview/print in a new tab */
  previewUndertakingPdf: (id: string) =>
    api.get<string>(`/requests/${id}/undertaking/preview`, { responseType: 'text' }).then((r) => r.data),

  /** Sends the undertaking PDF to the customer's registered email */
  sendUndertakingEmail: (id: string) =>
    api.post(`/requests/${id}/undertaking/send-email`).then((r) => r.data),

  /** Attaches the signed undertaking and places the request on hold */
  attachSignedUndertaking: (id: string, data: { signedDocumentUrl: string; holdDays: number; notes?: string }) =>
    api.post(`/requests/${id}/undertaking/attach`, data).then((r) => r.data),

  // ── Plot Annexation ─────────────────────────────────────────────────────
  /** Record plot annexation (additional adjacent land + fee) */
  requestAnnexation: (id: string, data: { additionalArea?: string; annexationFee: number; notes?: string }) =>
    api.post(`/requests/${id}/annexation`, data).then((r) => r.data),

  // ── Plot Merging ────────────────────────────────────────────────────────
  /** Merge an adjacent same-owner plot — package fee doubles for merged portion */
  requestPlotMerge: (id: string, data: { mergedPlotNumber: string; mergedPlotSector: string; mergedPlotSize?: string; notes?: string }) =>
    api.post(`/requests/${id}/plot-merge`, data).then((r) => r.data),

  // ── Per-Department CAD Assignments ─────────────────────────────────────
  /** Assign a CAD operator for the given department stage */
  assignCadOperator: (id: string, data: { cadType: string; assignedUserId: string }) =>
    api.post(`/requests/${id}/cad-assignments`, data).then((r) => r.data),

  /** Submit the CAD file for a specific department stage */
  submitDeptCadFile: (id: string, data: { cadType: string; fileUrl: string; fileName?: string; fileType?: string }) =>
    api.post(`/requests/${id}/cad-files/${data.cadType.toLowerCase()}`, {
      fileUrl:  data.fileUrl,
      fileName: data.fileName,
      fileType: data.fileType,
    }).then((r) => r.data),

  getPossessionCertPreviewUrl: (id: string) =>
    `${api.defaults.baseURL}/requests/${id}/possession-certificate/preview`,

  selectPackage: (id: string, packageId: string, interiorDesignPackageId?: string, supervisionPackageId?: string) =>
    api.post(`/requests/${id}/package`, { packageId, interiorDesignPackageId, supervisionPackageId }).then((r) => r.data),

  getPayment: (id: string) =>
    api.get(`/requests/${id}/payment`).then((r) => r.data as {
      challanNo: string; totalAmount: number; paidAmount: number;
      status: string; paidAt?: string; scannedFileUrl?: string;
    }),

  printPaymentChallan: (id: string) =>
    api.get(`/requests/${id}/payment/challan/print`, { responseType: 'text' }).then((r) => r.data as string),

  confirmPayment: (id: string, data: { challanNo?: string; amountPaid: number; scannedChallanFileUrl?: string }) =>
    api.post(`/requests/${id}/payment/confirm`, data).then((r) => r.data),

  // Backend expects JSON { fileUrl, notes } not multipart
  uploadPlan: (id: string, data: { fileUrl: string; notes?: string }) =>
    api.post<ArchitecturalPlan>(`/requests/${id}/plan`, data).then((r) => r.data),

  // Customer approves a specific plan version
  approvePlan: (id: string, planId: string) =>
    api.post(`/requests/${id}/plan/${planId}/approve`).then((r) => r.data),

  // Customer requests revision on a specific plan version
  requestPlanRevision: (id: string, planId: string, data: { comments: string; markupFileUrl?: string }) =>
    api.post(`/requests/${id}/plan/${planId}/revision`, data).then((r) => r.data),

  // 3D Visualization step
  threeDUploadFile: (id: string, data: { fileUrl: string; fileName: string; fileType: string; notes?: string }) =>
    api.post(`/requests/${id}/3d-visualization`, data).then((r) => r.data),

  threeDComplete: (id: string) =>
    api.post(`/requests/${id}/3d-visualization/complete`).then((r) => r.data),

  cadUploadFile: (id: string, data: { fileUrl: string; fileName: string; fileType: string; notes?: string }) =>
    api.post(`/requests/${id}/cad-files`, data).then((r) => r.data),

  cadComplete: (id: string) =>
    api.post(`/requests/${id}/cad-files/complete`).then((r) => r.data),

  structureComplete: (id: string, data: { fileUrl: string; observations?: string }) =>
    api.post(`/requests/${id}/structure/complete`, data).then((r) => r.data),

  mepComplete: (id: string, data: { fileUrl: string; observations?: string }) =>
    api.post(`/requests/${id}/mep/complete`, data).then((r) => r.data),

  principalApprove: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/principal-review/approve`, data).then((r) => r.data),

  paInitialReview: (id: string, data: {
    assignedArchitectId: string;
    soilTestFileUrl?: string;
    soilTestDate?: string;
    labName?: string;
    soilBearingCapacity?: string;
    resultSummary?: string;
    notes?: string;
  }) => api.post(`/requests/${id}/principal-architect/initial-review`, data).then((r) => r.data),

  principalSendBack: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/principal-review/send-back`, data).then((r) => r.data),

  uploadSoilTest: (id: string, data: {
    testDate: string; labName: string; soilBearingCapacity: string;
    resultSummary: string; reportFileUrl: string;
  }) => api.post(`/requests/${id}/soil-test`, data).then((r) => r.data),

  submitBuildingControl: (id: string, data: {
    surveyDate: string; officerName: string; observations?: string;
    violations?: string; suggestions?: string; photoUrlsJson?: string;
  }) => api.post(`/requests/${id}/building-control`, data).then((r) => r.data),

  finalApprove: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/final-approval/approve`, data).then((r) => r.data),

  finalReject: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/final-approval/reject`, data).then((r) => r.data),

  rejectRequest: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/reject`, data).then((r) => r.data),

  adminReview: (id: string, data: {
    action: string;
    rejectionReason?: string;
    allotmentLetterUrl?: string;
    cnicUrl?: string;
    messageScreenshotUrl?: string;
    eStampPaperUrl?: string;
    authorizedPersonCnicUrl?: string;
    authorizedPersonPhone?: string;
  }) =>
    api.post(`/requests/${id}/admin-review`, data).then((r) => r.data),

  initiateRequest: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/initiate`, data).then((r) => r.data),

  deliver: (id: string, data: { comments?: string }) =>
    api.post(`/requests/${id}/deliver`, data).then((r) => r.data),
};

// ─── Packages ────────────────────────────────────────────────────────────────

export const packagesService = {
  getAll: (params?: { plotType?: string; plotSize?: string; category?: string; designType?: string }) =>
    api.get<Package[]>('/packages', { params }).then((r) => r.data),

  getById: (id: string) =>
    api.get<Package>(`/packages/${id}`).then((r) => r.data),

  // Backend: POST /packages with CreatePackageDto
  create: (data: { plotType: string; plotSize: string; packageTier: string; lineItems: PackageLineItem[] }) =>
    api.post<Package>('/packages', data).then((r) => r.data),

  // Backend UpdatePackageDto expects { lineItems: [...] }
  update: (id: string, lineItems: PackageLineItem[]) =>
    api.put<Package>(`/packages/${id}`, { lineItems }).then((r) => r.data),
};

// ─── Tasks ───────────────────────────────────────────────────────────────────

export const tasksService = {
  getMyTasks: () =>
    api.get<TaskAssignment[]>('/tasks/my').then((r) => r.data),

  getForRequest: (requestId: string) =>
    api.get<TaskAssignment[]>(`/tasks/request/${requestId}`).then((r) => r.data),

  getDepartmentTasks: (deptId: string) =>
    api.get<TaskAssignment[]>(`/tasks/department/${deptId}`).then((r) => r.data),

  getUnassignedTasks: (deptId: string) =>
    api.get<TaskAssignment[]>(`/tasks/department/${deptId}/unassigned`).then((r) => r.data),

  getDepartmentWorkload: (deptId: string) =>
    api.get<EmployeeWorkload[]>(`/tasks/department/${deptId}/workload`).then((r) => r.data),

  // Manually assign a task to an employee (Admin)
  assignTask: (taskId: string, data: { employeeId: string; reason?: string }) =>
    api.post(`/tasks/${taskId}/assign`, data).then((r) => r.data),

  // Reassign an existing task to a different employee (Admin)
  reassignTask: (taskId: string, data: { newUserId: string; reason: string }) =>
    api.post(`/tasks/${taskId}/reassign`, data).then((r) => r.data),

  completeTask: (taskId: string) =>
    api.post(`/tasks/${taskId}/complete`).then((r) => r.data),

  getRoundRobin: (deptId: string) =>
    api.get(`/tasks/department/${deptId}/round-robin`).then((r) => r.data),

  resetRoundRobin: (deptId: string) =>
    api.post(`/tasks/department/${deptId}/round-robin/reset`).then((r) => r.data),

  setEmployeeAvailability: (userId: string, data: { isAvailable: boolean; reason?: string; unavailableUntil?: string }) =>
    api.put(`/employees/${userId}/availability`, data).then((r) => r.data),
};

// ─── Notifications ───────────────────────────────────────────────────────────
// Backend route: /api/v1/notifications (no customerId in path — uses JWT claim)

export const notificationsService = {
  getAll: (unreadOnly = false) =>
    api
      .get<CustomerNotification[]>('/notifications', { params: { unreadOnly } })
      .then((r) => r.data),

  markRead: (notifId: string) =>
    api.post(`/notifications/${notifId}/read`).then((r) => r.data),

  respond: (notifId: string, responseText: string) =>
    api
      .post(`/notifications/${notifId}/respond`, { responseText })
      .then((r) => r.data),
};

// ─── Support Tickets ─────────────────────────────────────────────────────────

export const ticketsService = {
  create: (data: {
    subject: string;
    category: string;
    description: string;
    attachmentUrl?: string;
    requestId?: string;
  }) => api.post<SupportTicket>('/tickets', data).then((r) => r.data),

  getMyTickets: () =>
    api.get<SupportTicket[]>('/tickets/my').then((r) => r.data),

  getAll: () => api.get<SupportTicket[]>('/tickets').then((r) => r.data),

  getDepartmentTickets: (deptId: string) =>
    api.get<SupportTicket[]>(`/tickets/department/${deptId}`).then((r) => r.data),

  getById: (ticketId: string) =>
    api.get<SupportTicket>(`/tickets/${ticketId}`).then((r) => r.data),

  addReply: (ticketId: string, data: { message: string; attachmentUrl?: string }) =>
    api.post<TicketReply>(`/tickets/${ticketId}/reply`, data).then((r) => r.data),

  closeTicket: (ticketId: string, satisfactionRating?: number) =>
    api
      .post(`/tickets/${ticketId}/close`, { satisfactionRating })
      .then((r) => r.data),

  assignTicket: (ticketId: string, departmentId: string) =>
    api
      .post(`/tickets/${ticketId}/assign`, { departmentId })
      .then((r) => r.data),

  // Technical Support operations
  getQueueTickets: () =>
    api.get<SupportTicket[]>('/tickets/queue').then((r) => r.data),

  getAssignedToMe: () =>
    api.get<SupportTicket[]>('/tickets/assigned-to-me').then((r) => r.data),

  resolveTicket: (ticketId: string) =>
    api.post(`/tickets/${ticketId}/resolve`).then((r) => r.data),

  reopenTicket: (ticketId: string) =>
    api.post(`/tickets/${ticketId}/reopen`).then((r) => r.data),

  assignToUser: (ticketId: string, userId: string) =>
    api.post(`/tickets/${ticketId}/assign-user`, { userId }).then((r) => r.data),
};

// ─── Templates ───────────────────────────────────────────────────────────────

export const templatesService = {
  getAll: () => api.get<Template[]>('/templates').then((r) => r.data),

  getById: (id: string) =>
    api.get<Template>(`/templates/${id}`).then((r) => r.data),

  update: (id: string, data: { content?: string; templateName?: string }) =>
    api.put<Template>(`/templates/${id}`, data).then((r) => r.data),
};

// ─── Survey Observations ─────────────────────────────────────────────────────

export const surveyService = {
  getByRequest: (requestId: string) =>
    api.get<SurveyObservation[]>(`/requests/${requestId}/surveys`).then((r) => r.data),
};

export const appointmentsService = {
  getAll: (params?: { departmentId?: string; status?: string; date?: string }) =>
    api.get<Appointment[]>('/appointments', { params }).then((r) => r.data),

  getById: (id: string) =>
    api.get<Appointment>(`/appointments/${id}`).then((r) => r.data),

  getAvailability: (departmentId: string, date: string, startTime: string, durationMinutes = 30) =>
    api.get<AvailableEmployee[]>('/appointments/availability', {
      params: { departmentId, date, startTime, durationMinutes },
    }).then((r) => r.data),

  book: (data: {
    customerId: string;
    requestId?: string;
    departmentId: string;
    assignedEmployeeId?: string;
    appointmentDate: string;
    startTime: string;
    durationMinutes: number;
    numberOfPersons: number;
    attendeeNames?: string;
    bookingMethod: string;
    purpose?: string;
    notes?: string;
  }) => api.post<Appointment>('/appointments', data).then((r) => r.data),

  complete: (id: string, notes?: string) =>
    api.patch(`/appointments/${id}/complete`, { notes }).then((r) => r.data),

  cancel: (id: string, notes?: string) =>
    api.patch(`/appointments/${id}/cancel`, { notes }).then((r) => r.data),

  noShow: (id: string) =>
    api.patch(`/appointments/${id}/no-show`).then((r) => r.data),

  assign: (id: string, employeeId: string | null) =>
    api.patch(`/appointments/${id}/assign`, { employeeId }).then((r) => r.data),
};
