// ─── Auth ────────────────────────────────────────────────────────────────────

export interface StaffLoginRequest {
  email: string;
  password: string;
}

export interface CustomerOtpRequest {
  cnic: string;
}

export interface CustomerVerifyOtpRequest {
  cnic: string;
  otp: string;
}

export interface AuthResponse {
  token: string;
  refreshToken?: string;
  user: User;
}

export interface CustomerAuthResponse {
  token: string;
  customer: Customer;
}

// ─── User / Auth State ───────────────────────────────────────────────────────

export type UserRole =
  | 'Admin'
  | 'PossessionAdmin'
  | 'DdfcAdmin'
  | 'ReceptionOfficer'
  | 'TransferOfficer'
  | 'FinanceOfficer'
  | 'TownPlanner'
  | 'BuildingControlOfficer'
  | 'Architect'
  | 'StructureEngineer'
  | 'MEPEngineer'
  | 'PrincipalArchitect'
  | 'DHADesignHead'
  | 'Technical Support';

export interface User {
  userId: string;
  fullName: string;
  email: string;
  roleId: string;
  roleName: UserRole | string;
  departmentId?: string;
  departmentName?: string;
  isActive: boolean;
  lastLogin?: string;
  permissions?: Record<string, boolean> | string | string[];
}

export interface Customer {
  customerId: string;
  fullName: string;
  cnic: string;
  phoneNumber: string;
  email?: string;
  address?: string;
  createdAt: string;
}

// ─── Enums ───────────────────────────────────────────────────────────────────

export type RequestStatus =
  | 'Submitted'
  | 'Initiated'
  | 'TransferApproved'
  | 'FinanceApproved'
  | 'BothBranchesCleared'
  | 'PossessionIssued'
  | 'PossessionLetterSigned'
  | 'DelayUndertakingRequested'
  | 'DelayUndertakingSigned'
  | 'PackageSelected'
  | 'PackagePaid'
  | 'ArchitectAssigned'
  | 'ArchitectureApproved'
  | 'ThreeDCompleted'
  | 'CadCompleted'
  | 'StructureCompleted'
  | 'MEPCompleted'
  | 'PrincipalArchitectApproved'
  | 'TownPlanningCompleted'
  | 'BuildingControlCompleted'
  | 'FinalApproved'
  | 'Delivered'
  | 'Rejected'
  | 'OnHold';

export type RequestType = 'PossessionDesign' | 'RevisedPlan' | 'AsBuiltPlan';
export type PlotType = 'Residential' | 'Commercial';
export type PlotSize = 'FourMarla' | 'FiveMarla' | 'EightMarla' | 'TenMarla' | 'OneKanal' | 'TwoKanal';
export type PackageTier = 'Bronze' | 'Silver' | 'Gold';
export type PackageCategory = 'HouseDesign' | 'InteriorDesign' | 'Supervision' | 'RevisedPlan' | 'AsBuiltPlan';
export type DesignType = 'InclusiveDesign' | 'ExclusiveDesign';
export type TicketStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed';
export type TicketCategory =
  | 'DocumentIssue'
  | 'PaymentIssue'
  | 'DesignQuery'
  | 'GeneralEnquiry'
  | 'Complaint';

// ─── Plot ────────────────────────────────────────────────────────────────────

export interface Plot {
  id: string;
  plotNumber: string;
  sectorNo: string;
  streetNo: string;
  phaseNo: string;
  plotSize: PlotSize;
  plotType: PlotType;
  currentStatus: string;
}

// ─── Possession Request ──────────────────────────────────────────────────────

export interface RequestDocument {
  documentId: string;
  documentType: string;
  fileUrl: string;
  uploadedAt: string;
}

export interface Payment {
  paymentId: string;
  challanNumber: string;
  amount: number;
  isPaid: boolean;
  dueDate?: string;
  paidAt?: string;
  challanPdfUrl?: string;
}

export interface RevisionEntry {
  note: string;
  requestedAt: string;
}

export interface SoilTestReport {
  reportId: string;
  reportUrl: string;
  uploadedAt: string;
}

export interface ThreeDVisualization {
  id: string;
  fileName: string;
  fileUrl: string;
  fileType: string;
  uploadedAt: string;
  notes?: string;
}

export interface CadFile {
  id: string;
  fileName: string;
  fileUrl: string;
  fileType: string;
  uploadedAt: string;
  notes?: string;
}

// ─── Per-Department CAD Assignment ───────────────────────────────────────────

export type CadType = 'Architecture' | 'ThreeD' | 'Structure' | 'MEP';

export interface CadAssignment {
  id: string;
  cadType: CadType;
  assignedUserId: string;
  assignedUserName?: string;
  assignedAt: string;
  fileUrl?: string;
  fileName?: string;
  fileType?: string;
  completedAt?: string;
}

export interface PossessionRequest {
  requestId: string;
  id: string;
  customerId: string;
  customerName?: string;
  cnic?: string;
  phoneNumber?: string;
  plotId: string;
  plotNumber?: string;
  sectorNo?: string;
  phaseNo?: string;
  plotType?: string;
  plotSize?: string;
  fileNo: string;
  membershipDPRNo: string;
  ownerTitle?: string;
  ownerName?: string;
  sonDaughterWifeOf?: string;
  guardianRelation?: string;
  contractor?: string;
  // Admin review step
  allotmentLetterUrl?: string;
  cnicUrl?: string;
  adminRejectionReason?: string;
  adminReviewedAt?: string;
  messageScreenshotUrl?: string;
  eStampPaperUrl?: string;
  authorizedPersonCnicUrl?: string;
  authorizedPersonPhone?: string;
  authorizedPersonName?: string;
  packageTier?: string;
  packageTotal?: number;
  selectedInteriorDesignPackageId?: string;
  interiorDesignPackageTier?: string;
  interiorDesignPackageTotal?: number;
  selectedSupervisionPackageId?: string;
  supervisionPackageTier?: string;
  supervisionPackageTotal?: number;
  totalFee?: number;
  challanNo?: string;
  paymentStatus?: string;
  status: RequestStatus;
  requestType?: RequestType;
  selectedPackageId?: string;
  selectedDesignType?: string;
  linkedPossessionRequestId?: string;
  submittedAt: string;
  updatedAt?: string;
  transferApproved?: boolean;
  financeApproved?: boolean;
  adcAmount?: number;
  structureCompleted?: boolean;
  mepCompleted?: boolean;
  townPlanningCompleted?: boolean;
  buildingControlCompleted?: boolean;
  workflowHistory?: WorkflowHistoryEntry[];
  activeWorkflowStepNames?: string[];
  documents?: RequestDocument[];
  payments?: Payment[];
  soilTestReport?: SoilTestReport;
  assignedArchitectId?: string;
  assignedArchitectName?: string;
  threeDVisualizations?: ThreeDVisualization[];
  cadFiles?: CadFile[];
  cadAssignments?: CadAssignment[];
  revisionHistory?: RevisionEntry[];
  delayUndertaking?: DelayUndertaking;
  architectUndertaking?: ArchitectUndertaking;
  plotAnnexation?: PlotAnnexation;
  plotMerging?: PlotMerging;
  possessionCertificate?: {
    handedOverBy?: string;
    validUntil?: string;
  };
}

export interface WorkflowHistoryEntry {
  historyId: string;
  requestId: string;
  fromStatus: RequestStatus | null;
  toStatus: RequestStatus;
  actionBy?: string;
  actorName?: string;
  comments?: string;
  timestamp: string;
}

// ─── Delay Undertaking ────────────────────────────────────────────────────────

export type DelayUndertakingInitiator = 'Customer' | 'DDFC';

export interface DelayUndertaking {
  id: string;
  /** Who triggered the undertaking — 'Customer' | 'DDFC' */
  initiatedBy: DelayUndertakingInitiator;
  requestedByUserId?: string;
  requestedAt?: string;
  signedByCustomerId?: string;
  signedAt?: string;
  delayReason?: string;
  expectedDelayDays?: number;
  undertakingDocumentUrl?: string;
  notes?: string;
}

// ─── Architecture Stage Undertaking ──────────────────────────────────────────

export interface ArchitectUndertaking {
  id: string;
  signedDocumentUrl?: string;
  holdDays?: number;
  holdStartDate?: string;
  holdEndDate?: string;
  signedAt?: string;
  notes?: string;
}

// ─── Plot Annexation ──────────────────────────────────────────────────────────

export interface PlotAnnexation {
  id: string;
  additionalArea?: string;
  annexationFee: number;
  notes?: string;
  requestedAt: string;
  approvedAt?: string;
  documentUrl?: string;
}

// ─── Plot Merging ────────────────────────────────────────────────────────────

export interface PlotMerging {
  id: string;
  mergedPlotNumber: string;
  mergedPlotSector: string;
  mergedPlotSize?: string;
  notes?: string;
  requestedAt: string;
  approvedAt?: string;
  documentUrl?: string;
}

export interface CreateRequestDto {
  customerId: string;
  plotId: string;
  plotNumber: string;
  sectorNo: string;
  phaseNo: string;
  fileNo: string;
  membershipDPRNo: string;
  ownerTitle: string;
  ownerName: string;
  sonDaughterWifeOf: string;
  guardianRelation: string;
  authorizedPersonName?: string;
  contractor?: string;
  requestType?: number; // 0=PossessionDesign, 1=RevisedPlan, 2=AsBuiltPlan
  linkedPossessionRequestId?: string;
}

// ─── Packages ────────────────────────────────────────────────────────────────

export interface Package {
  id: string;
  packageId: string;
  plotType: PlotType;
  plotSize: PlotSize;
  packageTier: PackageTier;
  packageCategory: PackageCategory;
  designType: DesignType;
  isActive: boolean;
  lineItems: PackageLineItem[];
}

export interface PackageLineItem {
  id: string;
  lineItemId: string;
  packageId: string;
  serviceName: string;
  amountDDFC: number;
  amountExclusive: number;
  isFree: boolean;
  sortOrder: number;
}

// ─── Payments ────────────────────────────────────────────────────────────────

export interface Payment {
  paymentId: string;
  requestId: string;
  challanNo?: string;
  totalAmount: number;
  paidAmount?: number;
  status: 'Pending' | 'Paid' | 'Overdue';
  paidAt?: string;
  challan?: PaymentChallan;
}

export interface PaymentChallan {
  challanId: string;
  paymentId: string;
  challanNumber: string;
  generatedAt: string;
  bankName: string;
  iban: string;
  accountTitle: string;
}

// ─── Documents / Plans ───────────────────────────────────────────────────────

export interface ArchitecturalPlan {
  planId: string;
  requestId: string;
  version: number;
  fileUrl: string;
  uploadedBy: string;
  uploadedAt: string;
  customerApproved: boolean;
  approvedAt?: string;
  revisions?: PlanRevision[];
}

export interface PlanRevision {
  revisionId: string;
  planId: string;
  requestedBy: string;
  comments: string;
  requestedAt: string;
  resolvedAt?: string;
}

export interface Document {
  documentId: string;
  requestId: string;
  stepName: string;
  docType: string;
  fileUrl: string;
  version: number;
  uploadedBy: string;
  uploadedAt: string;
  isArchived: boolean;
}

// ─── Task Assignment ─────────────────────────────────────────────────────────

export interface TaskAssignment {
  taskId: string;
  requestId: string;
  departmentId: string;
  departmentName?: string;
  assignedToUserId?: string;
  assignedToName?: string;
  assignedByUserId?: string;
  assignedAt?: string;
  assignmentMethod?: 'RoundRobin' | 'Manual';
  status: 'Pending' | 'InProgress' | 'Completed';
  taskType?: string;
  isCompleted?: boolean;
  completedAt?: string;
  request?: PossessionRequest;
  requestStatus?: RequestStatus;
  daysInQueue?: number;
}

// ─── Department ──────────────────────────────────────────────────────────────

export interface Department {
  departmentId: string;
  departmentName: string;
  departmentCode: string;
  headUserId?: string;
  headName?: string;
  employeeCount?: number;
  activeRequests?: number;
}

export interface EmployeeWorkload {
  userId: string;
  employeeName: string;
  assigned: number;
  inProgress: number;
  completedToday: number;
  avgCompletionTime?: number;
  isAvailable: boolean;
}

// ─── Notifications ───────────────────────────────────────────────────────────

export interface CustomerNotification {
  notificationId: string;
  customerId: string;
  requestId?: string;
  title: string;
  messageBody: string;
  requiresResponse: boolean;
  responseText?: string;
  respondedAt?: string;
  isRead: boolean;
  readAt?: string;
  sentAt: string;
  channel: 'SMS' | 'Email' | 'InApp';
}

// ─── Support Tickets ─────────────────────────────────────────────────────────

export interface SupportTicket {
  ticketId: string;
  customerId: string;
  customerName?: string;
  requestId?: string;
  subject: string;
  category: TicketCategory;
  description: string;
  attachmentUrl?: string;
  status: TicketStatus;
  assignedDepartmentId?: string;
  assignedDepartmentName?: string;
  assignedUserId?: string;
  assignedUserName?: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
  closedAt?: string;
  satisfactionRating?: number;
  replies?: TicketReply[];
}

export interface TicketReply {
  replyId: string;
  ticketId: string;
  authorId: string;
  authorName?: string;
  authorType: 'Staff' | 'Customer';
  messageBody: string;
  attachmentUrl?: string;
  createdAt: string;
}

// ─── Admin Dashboard ─────────────────────────────────────────────────────────

export interface AdminDashboardStats {
  totalRequestsToday: number;
  activeRequests: number;
  pendingPayments: number;
  deliveredThisMonth: number;
  requestsByStatus: { status: RequestStatus; count: number }[];
  departmentWorkload: { departmentName: string; pending: number }[];
  recentActivity: WorkflowHistoryEntry[];
  avgTurnaroundByStage: { stage: string; avgDays: number }[];
}

// ─── Appointments ─────────────────────────────────────────────────────────────

export type AppointmentStatus = 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow';
export type AppointmentBookingMethod = 'PhoneCall' | 'InPerson';

export interface Appointment {
  id: string;
  customerId: string;
  customerName?: string;
  customerPhone?: string;
  requestId?: string;
  requestCode?: string;
  departmentId: string;
  departmentName?: string;
  assignedEmployeeId?: string;
  assignedEmployeeName?: string;
  appointmentDate: string;  // yyyy-MM-dd
  startTime: string;        // HH:mm
  durationMinutes: number;
  numberOfPersons: number;
  attendeeNames?: string;
  bookingMethod: AppointmentBookingMethod;
  bookedByName?: string;
  status: AppointmentStatus;
  purpose?: string;
  notes?: string;
  cancellationReason?: string;
  createdAt: string;
}

export interface AvailableEmployee {
  id: string;
  fullName: string;
  email: string;
}

// ─── Role / Permissions ──────────────────────────────────────────────────────

export interface Role {
  roleId: string;
  roleName: string;
  permissions: Record<string, boolean> | string | string[];
}

// ─── Templates ───────────────────────────────────────────────────────────────

export interface Template {
  templateId: string;
  templateName: string;
  templateType: string;
  language: 'EN' | 'UR';
  content: string;
  version: number;
  updatedBy?: string;
  updatedAt: string;
}

// ─── Survey ──────────────────────────────────────────────────────────────────

export interface SurveyObservation {
  surveyId: string;
  requestId: string;
  departmentId: string;
  officerName: string;
  surveyDate: string;
  observations: string;
  suggestions?: string;
  violations?: string;
  photoUrls?: string[];
  soilTest?: SoilTestReport;
}

export interface SoilTestReport {
  soilTestId: string;
  requestId: string;
  testDate: string;
  labName: string;
  soilBearingCapacity: string;
  resultSummary: string;
  reportFileUrl: string;
  uploadedBy: string;
  uploadedAt: string;
}

// ─── Pagination ──────────────────────────────────────────────────────────────

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ─── API Error ───────────────────────────────────────────────────────────────

export interface ApiError {
  status: number;
  message: string;
  errors?: Record<string, string[]>;
}
