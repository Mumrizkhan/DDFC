import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { ArrowLeft, MessageSquare, Bell, CheckCircle, XCircle, RotateCcw, PlayCircle, FileText, Printer, CalendarDays } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchRequestById, approvePlan, principalApprove, principalSendBack, finalApprove, finalReject, deliverRequest, initiateRequest, rejectRequest } from '../../store/slices/requestsSlice';
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
import { ReceptionPanel } from './panels/ReceptionPanel';
import { AdminReviewPanel } from './panels/AdminReviewPanel';

import { PrincipalArchitectInitialPanel } from './panels/PrincipalArchitectInitialPanel';
import { BookAppointmentModal } from '../../components/appointments/BookAppointmentModal';
import { AnnexationModal } from '../../components/shared/AnnexationModal';
import { PlotMergingModal } from '../../components/shared/PlotMergingModal';
import { format } from 'date-fns';
import { toast } from 'react-toastify';
import api from '../../services/api';
import type { RequestStatus } from '../../types';

const TABS = ['Overview', 'Timeline', 'Documents', 'Actions', 'Notes'];

// Statuses where Annexation / Plot-Merge buttons are available (all pre-package steps)
const PRE_PACKAGE_STATUSES: RequestStatus[] = [
  'Submitted', 'Initiated', 'TransferApproved', 'FinanceApproved',
  'BothBranchesCleared', 'PossessionLetterSigned',
];

// Statuses where the Possession Letter has been signed (and cert should appear in Documents)
const CERT_ISSUED_STATUSES: RequestStatus[] = [
  'PossessionLetterSigned',
  'PackageSelected', 'PackagePaid', 'ArchitectAssigned', 'ArchitectureApproved', 'ThreeDCompleted',
  'StructureCompleted', 'MEPCompleted', 'PrincipalArchitectApproved',
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
  const dept = staffUser?.departmentName ?? '';

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
          toast.success('Request initiated — workflow started');
          break;
        case 'rejectRequest':
          await dispatch(rejectRequest({ id: req.id, comments: c })).unwrap();
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
      const res = await api.get<string>(`/requests/${req.id}/possession-certificate/preview`, {
        responseType: 'text',
      });
      const win = window.open('', '_blank', 'noopener,noreferrer');
      if (win) {
        win.document.write(res.data);
        win.document.close();
      } else {
        toast.warn('Popup blocked — please allow popups and try again');
      }
    } catch {
      toast.error('Failed to load certificate');
    }
  };

  const renderDepartmentPanel = () => {
    if (!dept) return null;
    const d = dept.toLowerCase();
    if (d.includes('architecture')) {
      if (req.status === 'ArchitectureApproved')
        return <ThreeDPanel request={req} requestId={req.id} />;
      return <ArchitecturePanel request={req} requestId={req.id} />;
    }
    if (d.includes('principal')) {
      if (req.status === 'PackagePaid' || req.status === 'ArchitectAssigned')
        return <PrincipalArchitectInitialPanel request={req} requestId={req.id} />;
      // else fall through to action-based panel (handled in Actions tab)
      return null;
    }
    if (d.includes('structure')) return <StructurePanel requestId={req.id} request={req} />;
    if (d.includes('mep')) return <MepPanel requestId={req.id} request={req} />;
    if (d.includes('building control')) {
      return <BuildingControlPanel request={req} requestId={req.id} />;
    }
    if (d.includes('administration') || d.includes('ddfc admin')) {
      const s = req.status;
      // After possession letter is signed, go directly to package selection
      if (s === 'PossessionLetterSigned' || s === 'PackageSelected') {
        return <ReceptionPanel request={req} requestId={req.id} />;
      }
      // BothBranchesCleared or PossessionIssued (legacy): show sign panel
      return <DdfcAdminPanel request={req} requestId={req.id} />;
    }
    if (d.includes('transfer')) return <TransferPanel requestId={req.id} />;
    if (d.includes('finance')) return <FinancePanel request={req} requestId={req.id} />;
    if (d.includes('reception') || d.includes('front desk')) return <ReceptionPanel request={req} requestId={req.id} />;
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
            <StatusBadge status={req.status} />
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
      <WorkflowStepper currentStatus={req.status} activeStepNames={req.activeWorkflowStepNames} />

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

          {/* Admin Review Panel — shown for Admin or Possession Admin when request is Submitted */}
          {req.status === 'Submitted' && (staffUser?.roleName === 'Admin' || staffUser?.roleName === 'Possession Admin') && (
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
                  <a
                    href={doc.fileUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-blue-600 text-sm hover:underline shrink-0 ml-3"
                  >
                    View
                  </a>
                </div>
              );
            })}

            {!CERT_ISSUED_STATUSES.includes(req.status) && (req.documents ?? []).length === 0 && (
              <p className="text-gray-400 text-center py-8">No documents uploaded</p>
            )}
          </div>
        </Card>
      )}

      {activeTab === 'Actions' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <Card title="Workflow Actions">
            <div className="space-y-3">
              {/* Submitted — Initiate or Reject (Reception Officer only) */}
              {req.status === 'Submitted' && (dept.toLowerCase().includes('reception') || dept.toLowerCase().includes('front desk')) && (
                <>
                  <Button
                    variant="primary"
                    className="w-full"
                    loading={actionLoading}
                    onClick={() => runAction('initiateRequest')}
                    icon={<PlayCircle size={16} />}
                  >
                    Initiate Request
                  </Button>
                  <Button
                    variant="danger"
                    className="w-full"
                    loading={actionLoading}
                    onClick={() => openActionWithComments('rejectRequest')}
                    icon={<XCircle size={16} />}
                  >
                    Reject Request
                  </Button>
                </>
              )}

              {/* Architecture — send plan for customer approval */}
              {dept.toLowerCase().includes('architecture') && (
                <>
                  <Button
                    variant="primary"
                    className="w-full"
                    loading={actionLoading}
                    onClick={handleApprovePlan}
                    icon={<CheckCircle size={16} />}
                  >
                    Approve Plan (Customer Review)
                  </Button>
                </>
              )}

              {/* Principal Architect actions */}
              {dept.toLowerCase().includes('principal') && (
                <>
                  <Button
                    variant="primary"
                    className="w-full"
                    loading={actionLoading}
                    onClick={() => runAction('principalApprove')}
                    icon={<CheckCircle size={16} />}
                  >
                    Principal Approve
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

              {/* DHA Design Head actions */}
              {dept.toLowerCase().includes('design') && (
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
                  <Button
                    variant="secondary"
                    className="w-full"
                    loading={actionLoading}
                    onClick={() => runAction('deliver')}
                    icon={<CheckCircle size={16} />}
                  >
                    Mark Delivered
                  </Button>
                  <Button
                    variant="outline"
                    className="w-full"
                    loading={actionLoading}
                    onClick={() => runAction('issuePossessionCert')}
                    icon={<CheckCircle size={16} />}
                  >
                    Issue Possession Certificate
                  </Button>
                </>
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
        title={pendingAction === 'principalSendBack' ? 'Send Back — Reason' : pendingAction === 'rejectRequest' ? 'Reject Request — Reason' : 'Final Reject — Reason'}
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
