import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { ArrowLeft, MessageSquare, Bell, CheckCircle, XCircle, RotateCcw, FileText, Printer, CalendarDays } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchRequestById, approvePlan, principalApprove, principalSendBack, finalApprove, finalReject, deliverRequest, initiateRequest, rejectRequest, adminReview } from '../../store/slices/requestsSlice';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/shared/StatusBadge';
import { WorkflowTimeline } from '../../components/shared/WorkflowTimeline';
import { WorkflowStepper } from '../../components/shared/WorkflowStepper';
import { Modal } from '../../components/ui/Modal';
import { Textarea } from '../../components/ui/Input';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { ArchitecturePanel } from './panels/ArchitecturePanel';
import { ThreeDPanel } from './panels/ThreeDPanel';
import { StructurePanel } from './panels/StructurePanel';
import { MepPanel } from './panels/MepPanel';
import { BuildingControlPanel } from './panels/BuildingControlPanel';
import { DdfcAdminPanel } from './panels/DdfcAdminPanel';
import { TransferPanel } from './panels/TransferPanel';
import { FinancePanel } from './panels/FinancePanel';
import { AdCoordPanel } from './panels/AdCoordPanel';
import { BcdPanel } from './panels/BcdPanel';
import { DocumentVerificationPanel } from './panels/DocumentVerificationPanel';
import { ReceptionPanel } from './panels/ReceptionPanel';
import { AdminReviewPanel } from './panels/AdminReviewPanel';

import { SoilTestPanel } from './panels/SoilTestPanel';
import { PrincipalArchitectInitialPanel } from './panels/PrincipalArchitectInitialPanel';
import { PrincipalArchitectDesignReviewPanel } from './panels/PrincipalArchitectDesignReviewPanel';
import { BookAppointmentModal } from '../../components/appointments/BookAppointmentModal';
import { AnnexationModal } from '../../components/shared/AnnexationModal';
import { PlotMergingModal } from '../../components/shared/PlotMergingModal';
import { format } from 'date-fns';
import { toast } from 'react-toastify';
import api from '../../services/api';
import { requestsService } from '../../services/endpoints';
import { viewWatermarkedFile, viewWatermarkedHtml } from '../../services/documentViewer';
import type { RequestStatus } from '../../types';

const TABS = ['Overview', 'Timeline', 'Documents', 'Actions', 'Notes'];

// Statuses where Annexation / Plot-Merge buttons are available (all pre-package steps)
const PRE_PACKAGE_STATUSES: RequestStatus[] = [
  'Submitted', 'DocumentsVerification', 'TransferApproved', 'FinanceApproved',
  'BothBranchesCleared', 'AdCoordApproved', 'BcdLetterUploaded', 'PossessionLetterSigned',
];

// Statuses where the Possession Letter has been signed (and cert should appear in Documents)
const CERT_ISSUED_STATUSES: RequestStatus[] = [
  'PossessionLetterSigned',
  'PackageSelected', 'PackagePaid', 'SoilTestCompleted', 'ArchitectAssigned', 'ArchitectureApproved', 'ThreeDCompleted',
  'StructureCompleted', 'MEPCompleted', 'PrincipalArchitectReviewPending', 'PrincipalArchitectApproved',
  'TownPlanningCompleted', 'BuildingControlCompleted', 'FinalApproved', 'Delivered',
];

export const TaskDetailPage: React.FC = () => {
  const { requestId } = useParams<{ requestId: string }>();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const { currentRequest, loading } = useAppSelector((s) => s.requests);
  const { staffUser } = useAppSelector((s) => s.auth);
  const initialTab = new URLSearchParams(location.search).get('tab') ?? 'Overview';
  const [activeTab, setActiveTab] = useState(initialTab);
  const [comments, setComments] = useState('');
  const [showCommentsModal, setShowCommentsModal] = useState(false);
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const [notifyMessage, setNotifyMessage] = useState('');
  const [actionLoading, setActionLoading] = useState(false);
  const [showBookAppt, setShowBookAppt] = useState(false);
  const [showAnnexation, setShowAnnexation] = useState(false);
  const [showMerging, setShowMerging] = useState(false);

  useEffect(() => {
    if (requestId) dispatch(fetchRequestById(requestId));
  }, [dispatch, requestId]);

  if (loading || !currentRequest) return <PageLoader />;

  const req = currentRequest;
  const hasPaymentChallan = Boolean(req.challanNo && req.selectedPackageId);
  const dept = staffUser?.departmentName ?? '';
  const activeSteps = req.activeWorkflowStepNames ?? [];
  // Falls back to true when WE tracking data is absent (e.g. seeder re-ran), so status checks still gate panels
  const stepActive = activeSteps.length === 0
    ? () => true
    : (partial: string) => activeSteps.some(s => s.toLowerCase().includes(partial.toLowerCase()));

  const handleApprovePlan = async () => {
    const planDoc = (req.documents ?? []).find((d) => d.documentType.toLowerCase().includes('plan'));
    if (!planDoc) { toast.error('No plan document found'); return; }
    setActionLoading(true);
    try {
      await dispatch(approvePlan({ requestId: req.id, planId: planDoc.documentId })).unwrap();
      toast.success('Plan approved');
    } catch {
      toast.error('Action failed');
    } finally {
      setActionLoading(false);
    }
  };

  const runAction = async (actionName: string, commentText?: string) => {
    setActionLoading(true);
    try {
      const c = commentText?.trim() || undefined;
      switch (actionName) {
        case 'principalApprove':
          await dispatch(principalApprove({ id: req.id, comments: c })).unwrap();
          toast.success('Principal review approved');
          break;
        case 'principalSendBack':
          await dispatch(principalSendBack({ id: req.id, comments: c })).unwrap();
          toast.success('Sent back for revision');
          break;
        case 'finalApprove':
          await dispatch(finalApprove({ id: req.id, comments: c })).unwrap();
          toast.success('Final approval granted');
          break;
        case 'finalReject':
          await dispatch(finalReject({ id: req.id, comments: c })).unwrap();
          toast.success('Request rejected');
          break;
        case 'deliver':
          await dispatch(deliverRequest({ id: req.id, comments: c })).unwrap();
          toast.success('Marked as delivered');
          break;
        case 'initiateRequest':
          await dispatch(initiateRequest({ id: req.id, comments: c })).unwrap();
          toast.success('Request submitted — workflow started');
          break;
        case 'rejectRequest':
          await dispatch(rejectRequest({ id: req.id, comments: c })).unwrap();
          toast.success('Request rejected');
          break;
        case 'adminApprove':
          await dispatch(adminReview({ id: req.id, data: { action: 'Initiate' } })).unwrap();
          toast.success('Request approved — workflow advanced');
          break;
        case 'adminReject':
          await dispatch(adminReview({ id: req.id, data: { action: 'Reject', rejectionReason: c } })).unwrap();
          toast.success('Request rejected');
          break;
      }
    } catch {
      toast.error('Action failed');
    } finally {
      setActionLoading(false);
      setShowCommentsModal(false);
      setComments('');
      setPendingAction(null);
    }
  };

  const openActionWithComments = (action: string) => {
    setPendingAction(action);
    setShowCommentsModal(true);
  };

  const handleViewCertificate = async () => {
    try {
      await viewWatermarkedHtml(async () => {
        const response = await api.get<string>(`/requests/${req.id}/possession-certificate/preview`, {
          responseType: 'text',
        });
        return response.data;
      }, staffUser?.fullName ?? '');
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to load certificate');
    }
  };

  const handleViewChallan = async () => {
    try {
      await viewWatermarkedHtml(() => requestsService.printPaymentChallan(req.id), staffUser?.fullName ?? '');
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to load payment challan');
    }
  };

  const handleViewFile = async (url: string) => {
    try {
      await viewWatermarkedFile(url, staffUser?.fullName ?? '');
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to load document');
    }
  };

  const renderDepartmentPanel = () => {
    if (staffUser?.roleName === 'Principal Architect' &&
        req.status === 'ThreeDDraftPending' && stepActive('assign 3d drafter'))
      return <ArchitecturePanel request={req} requestId={req.id} />;
    if (!dept) return null;
    const d = dept.toLowerCase();
    if (d.includes('3d') || d.includes('three d')) {
      if (req.status === 'ArchitectureApproved' && stepActive('3d visualization'))
        return <ThreeDPanel request={req} requestId={req.id} />;
      return null;
    }
    if (d.includes('architecture')) {
      if (!stepActive('architect department') && !stepActive('architecture department') && !stepActive('3d department') && !stepActive('assign 3d drafter') && req.status !== 'ArchitectAssigned' && req.status !== 'ThreeDDraftPending') return null;
      if (req.status === 'ThreeDDraftPending' || stepActive('assign 3d drafter'))
        return <ArchitecturePanel request={req} requestId={req.id} />;
      if (req.status === 'ArchitectureApproved' && !stepActive('architect department'))
        return null;
      return <ArchitecturePanel request={req} requestId={req.id} />;
    }
    if (d.includes('principal')) {
      if (!stepActive('principal architect')) return null;
      if (req.status === 'SoilTestCompleted' || req.status === 'PackagePaid')
        return <PrincipalArchitectInitialPanel request={req} requestId={req.id} />;
      if (req.status === 'PrincipalArchitectReviewPending' || req.status === 'PrincipalArchitectApproved')
        return <PrincipalArchitectDesignReviewPanel request={req} />;
      return null;
    }
    if (d.includes('structure')) {
      if (!stepActive('structure')) return null;
      return <StructurePanel requestId={req.id} request={req} />;
    }
    if (d.includes('mep')) {
      if (!stepActive('mep')) return null;
      return <MepPanel requestId={req.id} request={req} />;
    }
    if (d.includes('building control')) {
      if (req.status === 'PackagePaid' && stepActive('soil test'))
        return <SoilTestPanel request={req} requestId={req.id} />;
      if (req.status === 'AdCoordApproved' && stepActive('bcd'))
        return <BcdPanel requestId={req.id} />;
      if (!stepActive('building control')) return null;
      return <BuildingControlPanel request={req} requestId={req.id} />;
    }
    if (d.includes('administration') || d.includes('ddfc admin')) {
      const s = req.status;
      if (s === 'DocumentsVerification' && stepActive('documents verification'))
        return <DocumentVerificationPanel request={req} requestId={req.id} />;
      if (s === 'AdminReviewPending' && stepActive('post-payment review'))
        return <AdminReviewPanel request={req} requestId={req.id} />;
      if ((s === 'BcdLetterUploaded' || s === 'PossessionIssued') && stepActive('ddfc admin'))
        return <DdfcAdminPanel request={req} requestId={req.id} />;
      if ((s === 'PossessionLetterSigned' && stepActive('package selection')) ||
          (s === 'PackageSelected' && stepActive('payment confirmation')))
        return <ReceptionPanel request={req} requestId={req.id} />;
      return null;
    }
    if (d.includes('transfer')) {
      if (!stepActive('transfer')) return null;
      return <TransferPanel requestId={req.id} />;
    }
    if (d.includes('finance')) {
      if (!stepActive('finance')) return null;
      return <FinancePanel request={req} requestId={req.id} />;
    }
    if (d.includes('ad coord')) {
      if (!stepActive('ad coordinator')) return null;
      return <AdCoordPanel requestId={req.id} />;
    }
    if (d.includes('reception') || d.includes('front desk')) {
      if (req.status === 'Submitted' && (stepActive('reception') || stepActive('document review'))) {
        return <ReceptionPanel request={req} requestId={req.id} />;
      }
      if (!stepActive('reception')) return null;
      return <ReceptionPanel request={req} requestId={req.id} />;
    }
    return null;
  };

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate(-1)}
          className="text-gray-500 hover:text-gray-800 transition-colors"
        >
          <ArrowLeft size={20} />
        </button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-bold text-gray-900 font-mono">{req.requestId}</h1>
            <StatusBadge status={req.status} activeStepNames={req.activeWorkflowStepNames} />
          </div>
          <p className="text-sm text-gray-500">
            {req.customerName} • Plot {req.plotNumber}, Sector {req.sectorNo} •{' '}
            {req.plotType} / {req.plotSize}
          </p>
        </div>
        <Button
          variant="secondary"
          size="sm"
          icon={<CalendarDays size={14} />}
          onClick={() => setShowBookAppt(true)}
        >
          Book Appointment
        </Button>
      </div>

      {/* Workflow Stepper */}
      <WorkflowStepper currentStatus={req.status} activeStepNames={req.activeWorkflowStepNames} requestType={req.requestType} />

      {/* Tabs */}
      <div className="flex gap-1 border-b border-gray-200">
        {TABS.map((tab) => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`px-4 py-2 text-sm font-medium transition-colors ${
              activeTab === tab
                ? 'border-b-2 border-blue-600 text-blue-600'
                : 'text-gray-500 hover:text-gray-800'
            }`}
          >
            {tab}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      {activeTab === 'Overview' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {/* Form 1 Fields */}
          <Card title="Request Details">
            <dl className="grid grid-cols-2 gap-x-4 gap-y-3 text-sm">
              {[
                ['File No', req.fileNo],
                ['CNIC', req.cnic],
                ['Phone', req.phoneNumber],
                ['Plot Number', req.plotNumber],
                ['Sector', req.sectorNo],
                ['Phase', req.phaseNo],
                ['Plot Type', req.plotType],
                ['Plot Size', req.plotSize],
                ['Submitted', format(new Date(req.submittedAt), 'dd MMM yyyy')],
                ['Updated', req.updatedAt ? format(new Date(req.updatedAt), 'dd MMM yyyy') : '—'],
              ].map(([label, value]) => (
                <React.Fragment key={label}>
                  <dt className="text-gray-500">{label}</dt>
                  <dd className="font-medium text-gray-800">{value ?? '—'}</dd>
                </React.Fragment>
              ))}
            </dl>
          </Card>

          {/* Package Info */}
          {req.packageTier && (
            <Card title="Package">
              <dl className="grid grid-cols-2 gap-x-4 gap-y-3 text-sm">
                <dt className="text-gray-500">Tier</dt>
                <dd className="font-medium">{req.packageTier}</dd>
                <dt className="text-gray-500">Total Fee</dt>
                <dd className="font-medium">PKR {req.totalFee?.toLocaleString()}</dd>
                <dt className="text-gray-500">Payment</dt>
                <dd className="font-medium">{req.paymentStatus ?? '—'}</dd>
              </dl>
            </Card>
          )}

          {/* Department-specific panel */}
          {renderDepartmentPanel() && (
            <div className="lg:col-span-2">{renderDepartmentPanel()}</div>
          )}

          {/* Plot Modifications — Annexation & Merging (available in all pre-package steps) */}
          {PRE_PACKAGE_STATUSES.includes(req.status) && (
            <div className="lg:col-span-2">
              <Card title="Plot Modifications">
                <div className="flex flex-wrap gap-3 items-center">
                  {/* Annexation */}
                  <button
                    onClick={() => setShowAnnexation(true)}
                    className={`flex items-center gap-2 px-4 py-2.5 rounded-lg border-2 text-sm font-medium transition-all
                      ${req.plotAnnexation
                        ? 'border-blue-400 bg-blue-50 text-blue-700'
                        : 'border-dashed border-gray-300 text-gray-600 hover:border-blue-400 hover:text-blue-600'
                      }`}
                  >
                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5v-4m0 4h-4m4 0l-5-5" />
                    </svg>
                    {req.plotAnnexation ? (
                      <span>
                        Annexation
                        <span className="ml-1.5 text-xs bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded-full">
                          PKR {req.plotAnnexation.annexationFee.toLocaleString()}
                        </span>
                      </span>
                    ) : (
                      'Add Annexation'
                    )}
                  </button>

                  {/* Plot Merging */}
                  <button
                    onClick={() => setShowMerging(true)}
                    className={`flex items-center gap-2 px-4 py-2.5 rounded-lg border-2 text-sm font-medium transition-all
                      ${req.plotMerging
                        ? 'border-violet-400 bg-violet-50 text-violet-700'
                        : 'border-dashed border-gray-300 text-gray-600 hover:border-violet-400 hover:text-violet-600'
                      }`}
                  >
                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
                    </svg>
                    {req.plotMerging ? (
                      <span>
                        Plot Merged
                        <span className="ml-1.5 text-xs bg-violet-100 text-violet-700 px-1.5 py-0.5 rounded-full">
                          {req.plotMerging.mergedPlotNumber} / {req.plotMerging.mergedPlotSector}
                        </span>
                      </span>
                    ) : (
                      'Merge Plot'
                    )}
                  </button>

                  <p className="text-xs text-gray-400 ml-auto">
                    Available until Package Selection
                  </p>
                </div>
              </Card>
            </div>
          )}

          {/* Admin Review Panel — role-based fallback for Admin/Possession Admin not in administration dept */}
          {req.status === 'DocumentsVerification'
            && stepActive('documents verification')
            && (staffUser?.roleName === 'Admin' || staffUser?.roleName === 'Possession Admin')
            && !dept.toLowerCase().includes('administration')
            && (
            <div className="lg:col-span-2">
              <DocumentVerificationPanel request={req} requestId={req.id} />
            </div>
          )}

          {req.status === 'AdminReviewPending'
            && stepActive('post-payment review')
            && (staffUser?.roleName === 'Admin' || staffUser?.roleName === 'Possession Admin')
            && !dept.toLowerCase().includes('administration')
            && (
            <div className="lg:col-span-2">
              <AdminReviewPanel request={req} requestId={req.id} />
            </div>
          )}
        </div>
      )}

      {activeTab === 'Timeline' && (
        <Card title="Workflow History">
          <WorkflowTimeline history={req.workflowHistory ?? []} />
        </Card>
      )}

      {activeTab === 'Documents' && (
        <Card title="Documents">
          <div className="space-y-2">
            {hasPaymentChallan && (
              <div className="flex items-center justify-between gap-3 rounded-lg border border-gray-200 bg-gray-50 p-3">
                <div className="flex min-w-0 items-center gap-2">
                  <FileText className="h-4 w-4 shrink-0 text-gray-500" />
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-gray-800">Payment Challan</p>
                    <p className="break-all text-xs text-gray-500">{req.challanNo}</p>
                  </div>
                </div>
                <button onClick={handleViewChallan} className="flex shrink-0 items-center gap-1 text-sm text-blue-600 hover:underline">
                  <Printer className="h-3.5 w-3.5" /> View / Print
                </button>
              </div>
            )}
            {/* Possession Certificate — virtual entry once issued */}
            {CERT_ISSUED_STATUSES.includes(req.status) && (
              <div className="flex items-center justify-between p-3 bg-amber-50 border border-amber-200 rounded-lg">
                <div className="flex items-center gap-2">
                  <FileText className="w-4 h-4 text-amber-600" />
                  <div>
                    <p className="text-sm font-medium text-gray-800">Possession Certificate</p>
                    <p className="text-xs text-amber-600">Official A4 document • Building Control</p>
                  </div>
                </div>
                <button
                  onClick={handleViewCertificate}
                  className="flex items-center gap-1 text-blue-600 text-sm hover:underline"
                >
                  <Printer className="w-3.5 h-3.5" /> View / Print
                </button>
              </div>
            )}

            {/* Uploaded documents — includes plans, reports, and all step uploads */}
            {(req.documents ?? []).map((doc) => {
              const isApproved = doc.documentType.includes('✓');
              const isPlan = doc.documentType.toLowerCase().includes('architectural');
              const isReport = doc.documentType.toLowerCase().includes('report');
              return (
                <div
                  key={doc.documentId}
                  className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                >
                  <div className="flex items-center gap-2 min-w-0">
                    <FileText className={`w-4 h-4 shrink-0 ${isPlan ? 'text-blue-500' : isReport ? 'text-green-500' : 'text-gray-400'}`} />
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-gray-800 truncate">{doc.documentType}</p>
                      <p className="text-xs text-gray-400">
                        {format(new Date(doc.uploadedAt), 'dd MMM yyyy HH:mm')}
                        {isApproved && <span className="ml-2 text-green-600 font-medium">Customer Approved</span>}
                      </p>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleViewFile(doc.fileUrl)}
                    className="text-blue-600 text-sm hover:underline shrink-0 ml-3"
                  >
                    View
                  </button>
                </div>
              );
            })}

            {!hasPaymentChallan && !CERT_ISSUED_STATUSES.includes(req.status) && (req.documents ?? []).length === 0 && (
              <p className="text-gray-400 text-center py-8">No documents uploaded</p>
            )}
          </div>
        </Card>
      )}

      {activeTab === 'Actions' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {/* Admin Review Panel in Actions tab — role-based fallback for admin users not in administration dept */}
          {req.status === 'DocumentsVerification'
            && stepActive('documents verification')
            && (staffUser?.roleName === 'Admin' || staffUser?.roleName === 'Possession Admin')
            && !dept.toLowerCase().includes('administration')
            && (
            <div className="lg:col-span-2">
              <AdminReviewPanel request={req} requestId={req.id} />
            </div>
          )}

          {/* Department-specific panel — same as Overview so all actions are accessible here too */}
          {renderDepartmentPanel() && (
            <div className="lg:col-span-2">{renderDepartmentPanel()}</div>
          )}

          <Card title="Workflow Actions">
            <div className="space-y-3">
              {/* ── Reception / Front Desk – status-tracking for possession process ── */}
              {(staffUser?.roleName === 'Reception Officer' || dept.toLowerCase().includes('reception') || dept.toLowerCase().includes('front desk')) && (
                <>
                        {req.status === 'Submitted' && (
                    <p className="text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-lg p-3 text-center">
                          ⏳ Request is being prepared for <strong>Documents Verification</strong>.
                    </p>
                  )}
                  {req.status === 'DocumentsVerification' && stepActive('documents verification') && (
                    <p className="text-sm text-blue-700 bg-blue-50 border border-blue-200 rounded-lg p-3 text-center">
                      ⏳ <strong>Reception</strong> is verifying the submitted documents.
                    </p>
                  )}
                  {req.status === 'DocumentsVerification' && stepActive('transfer') && (
                    <p className="text-sm text-blue-700 bg-blue-50 border border-blue-200 rounded-lg p-3 text-center">
                      🔄 <strong>Transfer Branch</strong> is reviewing the request.
                    </p>
                  )}
                  {(req.status === 'TransferApproved' || req.status === 'FinanceApproved') && (
                    <p className="text-sm text-blue-700 bg-blue-50 border border-blue-200 rounded-lg p-3 text-center">
                      🔄 <strong>Finance Branch</strong> is reviewing the request.
                    </p>
                  )}
                  {req.status === 'BothBranchesCleared' && (
                    <p className="text-sm text-purple-700 bg-purple-50 border border-purple-200 rounded-lg p-3 text-center">
                      🔄 <strong>AD Coordinator</strong> is reviewing the request.
                    </p>
                  )}
                  {req.status === 'AdCoordApproved' && (
                    <p className="text-sm text-purple-700 bg-purple-50 border border-purple-200 rounded-lg p-3 text-center">
                      ⏳ Pending <strong>BCD Possession Letter Upload</strong>.
                    </p>
                  )}
                  {req.status === 'BcdLetterUploaded' && (
                    <p className="text-sm text-purple-700 bg-purple-50 border border-purple-200 rounded-lg p-3 text-center">
                      ⏳ Pending <strong>DDFC Admin Possession Letter</strong> signing.
                    </p>
                  )}
                  {req.status === 'TownPlanningCompleted' && (
                    <p className="text-sm text-indigo-700 bg-indigo-50 border border-indigo-200 rounded-lg p-3 text-center">
                      ⏳ Awaiting <strong>Possession Certificate</strong> issuance by Town Planning.
                    </p>
                  )}
                  {(req.status === 'PossessionIssued' || req.status === 'FinalApproved')
                    && stepActive('document delivery') && (
                    <Button
                      variant="primary"
                      className="w-full"
                      loading={actionLoading}
                      onClick={() => runAction('deliver')}
                      icon={<CheckCircle size={16} />}
                    >
                      Mark as Delivered
                    </Button>
                  )}
                  {req.status === 'Rejected' && (
                    <p className="text-sm text-red-700 bg-red-50 border border-red-200 rounded-lg p-3 text-center">
                      ❌ This request was <strong>rejected</strong>.
                    </p>
                  )}
                  {req.status === 'Delivered' && (
                    <p className="text-sm text-green-700 bg-green-50 border border-green-200 rounded-lg p-3 text-center">
                      ✅ Request has been <strong>delivered</strong> to the customer.
                    </p>
                  )}
                </>
              )}

              {/* ── Step 10: Architecture – Approve Plan for Customer Review ─ */}
              {dept.toLowerCase().includes('architecture') && (req.status === 'ArchitectAssigned' || req.status === 'ArchitectureApproved')
                && stepActive('architect department') && (
                <Button
                  variant="primary"
                  className="w-full"
                  loading={actionLoading}
                  onClick={handleApprovePlan}
                  icon={<CheckCircle size={16} />}
                >
                  Approve Plan (Customer Review)
                </Button>
              )}

              {/* ── Step 14: Principal Architect – Design Review ─────────── */}
              {dept.toLowerCase().includes('principal') && (req.status === 'PrincipalArchitectReviewPending' || req.status === 'PrincipalArchitectApproved')
                && stepActive('principal architect') && (
                <>
                  <Button
                    variant="primary"
                    className="w-full"
                    loading={actionLoading}
                    onClick={() => runAction('principalApprove')}
                    icon={<CheckCircle size={16} />}
                  >
                    Approve Designs
                  </Button>
                  <Button
                    variant="outline"
                    className="w-full"
                    loading={actionLoading}
                    onClick={() => openActionWithComments('principalSendBack')}
                    icon={<RotateCcw size={16} />}
                  >
                    Send Back for Revision
                  </Button>
                </>
              )}

              {/* ── Step 3: Transfer Branch – NOC/NDC Review ────────────── */}
              {dept.toLowerCase().includes('transfer') && req.status === 'DocumentsVerification' && stepActive('transfer') && (
                <p className="text-xs text-blue-600 text-center py-2">
                  Use the <strong>Transfer Branch Panel</strong> above to Approve, Reject, or Request Clarification.
                </p>
              )}

              {/* ── Step 4: Finance Branch – Dues Clearance ──────────────── */}
              {dept.toLowerCase().includes('finance') && (req.status === 'DocumentsVerification' || req.status === 'TransferApproved') && stepActive('finance') && (
                <p className="text-xs text-blue-600 text-center py-2">
                  Use the <strong>Finance Branch Panel</strong> above to Approve or Reject dues clearance.
                </p>
              )}

              {/* ── Step 4b: AD Coordinator – Review ──────────────────────── */}
              {dept.toLowerCase().includes('ad coord') && req.status === 'BothBranchesCleared' && stepActive('ad coordinator') && (
                <p className="text-xs text-blue-600 text-center py-2">
                  Use the <strong>AD Coordinator Panel</strong> above to Approve or Reject the request.
                </p>
              )}

              {/* ── Step 5: DDFC Admin – Sign Possession Letter ──────────── */}
              {(dept.toLowerCase().includes('administration') || dept.toLowerCase().includes('ddfc admin'))
                && (req.status === 'BcdLetterUploaded' || req.status === 'PossessionIssued')
                && stepActive('ddfc admin') && (
                <p className="text-xs text-blue-600 text-center py-2">
                  Use the <strong>DDFC Admin Panel</strong> above to sign the Possession Letter.
                </p>
              )}

              {/* ── Step 4c: BCD – Upload Possession Letter ──────────────── */}
              {dept.toLowerCase().includes('building control') && req.status === 'AdCoordApproved' && stepActive('bcd') && (
                <p className="text-xs text-blue-600 text-center py-2">
                  Use the <strong>BCD Panel</strong> above to upload the possession letter.
                </p>
              )}

              {/* ── Documents Verification ─────────────────────────────── */}
              {(dept.toLowerCase().includes('administration') || dept.toLowerCase().includes('ddfc admin')
                || staffUser?.roleName === 'Admin' || staffUser?.roleName === 'Possession Admin')
                && req.status === 'DocumentsVerification' && stepActive('documents verification') && (
                <p className="text-xs text-blue-600 text-center py-2">
                  Use the <strong>Documents Verification Panel</strong> above to mark each document complete or incomplete.
                </p>
              )}

              {/* ── Post-payment Admin Review ───────────────────────────── */}
              {(dept.toLowerCase().includes('administration') || dept.toLowerCase().includes('ddfc admin')
                || staffUser?.roleName === 'Admin' || staffUser?.roleName === 'Possession Admin')
                && req.status === 'AdminReviewPending' && stepActive('post-payment review') && (
                <p className="text-xs text-blue-600 text-center py-2">
                  Use the <strong>Admin Review Panel</strong> above to approve or reject the paid request.
                </p>
              )}

              {/* ── Step 16: DHA Design Head – Final Approval ─────────────── */}
              {dept.toLowerCase().includes('design') && req.status === 'BuildingControlCompleted'
                && stepActive('dha design head') && (
                <>
                  <Button
                    variant="primary"
                    className="w-full"
                    loading={actionLoading}
                    onClick={() => runAction('finalApprove')}
                    icon={<CheckCircle size={16} />}
                  >
                    Final Approve
                  </Button>
                  <Button
                    variant="danger"
                    className="w-full"
                    loading={actionLoading}
                    onClick={() => openActionWithComments('finalReject')}
                    icon={<XCircle size={16} />}
                  >
                    Final Reject
                  </Button>
                </>
              )}

              {dept.toLowerCase().includes('building control') && req.status === 'PackagePaid' && stepActive('soil test') && (
                <p className="rounded-lg border border-blue-200 bg-blue-50 p-3 text-sm text-blue-800">
                  Submit the report in the Soil Test panel above to advance to Principal Architect Initial Review.
                </p>
              )}

              {/* Empty state — shown when no action condition above matches */}
              {!(
                (staffUser?.roleName === 'Reception Officer' || dept.toLowerCase().includes('reception') || dept.toLowerCase().includes('front desk')) ||
                (dept.toLowerCase().includes('architecture') && (req.status === 'ArchitectAssigned' || req.status === 'ArchitectureApproved') && stepActive('architect department')) ||
                (dept.toLowerCase().includes('principal') && (req.status === 'PrincipalArchitectReviewPending' || req.status === 'PrincipalArchitectApproved') && stepActive('principal architect')) ||
                (dept.toLowerCase().includes('design') && req.status === 'BuildingControlCompleted' && stepActive('dha design head')) ||
                (dept.toLowerCase().includes('transfer') && req.status === 'DocumentsVerification' && stepActive('transfer')) ||
                (dept.toLowerCase().includes('finance') && (req.status === 'DocumentsVerification' || req.status === 'TransferApproved') && stepActive('finance')) ||
                (dept.toLowerCase().includes('ad coord') && req.status === 'BothBranchesCleared' && stepActive('ad coordinator')) ||
                (dept.toLowerCase().includes('building control') && req.status === 'AdCoordApproved' && stepActive('bcd')) ||
                (dept.toLowerCase().includes('building control') && req.status === 'PackagePaid' && stepActive('soil test')) ||
                ((dept.toLowerCase().includes('administration') || dept.toLowerCase().includes('ddfc admin') ||
                  staffUser?.roleName === 'Admin' || staffUser?.roleName === 'Possession Admin') &&
                  req.status === 'DocumentsVerification' && stepActive('documents verification')) ||
                ((dept.toLowerCase().includes('administration') || dept.toLowerCase().includes('ddfc admin')) &&
                  (req.status === 'BcdLetterUploaded' || req.status === 'PossessionIssued') && stepActive('ddfc admin')) ||
                ((dept.toLowerCase().includes('administration') || dept.toLowerCase().includes('ddfc admin') ||
                  staffUser?.roleName === 'Admin' || staffUser?.roleName === 'Possession Admin') &&
                  req.status === 'AdminReviewPending' && stepActive('post-payment review'))
              ) && (
                <p className="text-sm text-gray-400 text-center py-4">
                  No workflow actions available for the current step.
                  {(renderDepartmentPanel()) ? ' Use the panel above to take action.' : ''}
                </p>
              )}
            </div>
          </Card>

          {/* Notify Customer */}
          <Card title="Notify Customer">
            <Textarea
              placeholder="Write a message to send to the customer..."
              value={notifyMessage}
              onChange={(e) => setNotifyMessage(e.target.value)}
              rows={4}
            />
            <Button
              variant="secondary"
              className="mt-3"
              disabled={!notifyMessage.trim()}
              icon={<Bell size={16} />}
            >
              Send Notification
            </Button>
          </Card>
        </div>
      )}

      {activeTab === 'Notes' && (
        <Card title="Internal Notes">
          <div className="text-gray-400 text-center py-12">
            <MessageSquare size={32} className="mx-auto mb-2 opacity-40" />
            <p>Internal notes coming soon</p>
          </div>
        </Card>
      )}

      {/* Comments Modal for actions requiring reason */}
      <Modal
        isOpen={showCommentsModal}
        onClose={() => { setShowCommentsModal(false); setPendingAction(null); setComments(''); }}
        title={pendingAction === 'principalSendBack' ? 'Send Back — Reason' : pendingAction === 'rejectRequest' ? 'Reject Request — Reason' : pendingAction === 'adminReject' ? 'Reject — Reason' : 'Final Reject — Reason'}
        footer={
          <div className="flex gap-2 justify-end">
            <Button variant="ghost" onClick={() => { setShowCommentsModal(false); setPendingAction(null); setComments(''); }}>
              Cancel
            </Button>
            <Button
              variant="primary"
              loading={actionLoading}
              onClick={() => pendingAction && runAction(pendingAction, comments)}
            >
              Confirm
            </Button>
          </div>
        }
      >
        <Textarea
          label="Comments"
          placeholder="Explain the reason for this action..."
          value={comments}
          onChange={(e) => setComments(e.target.value)}
          rows={4}
        />
      </Modal>

      <BookAppointmentModal
        isOpen={showBookAppt}
        onClose={() => setShowBookAppt(false)}
        customerId={req.customerId}
        requestId={req.id}
      />

      <AnnexationModal
        isOpen={showAnnexation}
        onClose={() => setShowAnnexation(false)}
        request={req}
      />

      <PlotMergingModal
        isOpen={showMerging}
        onClose={() => setShowMerging(false)}
        request={req}
      />
    </div>
  );
};
