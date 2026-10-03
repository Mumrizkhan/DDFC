import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Download, CheckCircle, RotateCcw, FileSignature } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchRequestById, approvePlan, requestPlanRevision, signDelayUndertaking } from '../../store/slices/requestsSlice';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/shared/StatusBadge';
import { WorkflowStepper } from '../../components/shared/WorkflowStepper';
import { WorkflowTimeline } from '../../components/shared/WorkflowTimeline';
import { Modal } from '../../components/ui/Modal';
import { Textarea } from '../../components/ui/Input';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { format } from 'date-fns';
import { toast } from 'react-toastify';

export const RequestDetailPage: React.FC = () => {
  const { requestId } = useParams<{ requestId: string }>();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { currentRequest, loading } = useAppSelector((s) => s.requests);
  const [revisionNote, setRevisionNote] = useState('');
  const [showRevisionModal, setShowRevisionModal] = useState(false);
  const [undertakingNotes, setUndertakingNotes] = useState('');
  const [showUndertakingModal, setShowUndertakingModal] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    if (requestId) dispatch(fetchRequestById(requestId));
  }, [dispatch, requestId]);

  if (loading || !currentRequest) return <PageLoader />;

  const req = currentRequest;

  const canApprove = req.status === 'ArchitectureApproved';
  const hasPlan = (req.documents ?? []).some((d) =>
    d.documentType.toLowerCase().includes('plan')
  );

  const handleApprove = async () => {
    const planDoc = (req.documents ?? []).find((d) => d.documentType.toLowerCase().includes('plan'));
    if (!planDoc) { toast.error('No plan document found'); return; }
    setActionLoading(true);
    try {
      await dispatch(approvePlan({ requestId: req.requestId, planId: planDoc.documentId })).unwrap();
      toast.success('Plan approved successfully!');
    } catch {
      toast.error('Failed to approve plan');
    } finally {
      setActionLoading(false);
    }
  };

  const handleRevision = async () => {
    if (!revisionNote.trim()) return;
    const planDoc = (req.documents ?? []).find((d) => d.documentType.toLowerCase().includes('plan'));
    if (!planDoc) { toast.error('No plan document found'); return; }
    setActionLoading(true);
    try {
      await dispatch(requestPlanRevision({ requestId: req.requestId, planId: planDoc.documentId, comments: revisionNote })).unwrap();
      toast.success('Revision request submitted');
      setShowRevisionModal(false);
      setRevisionNote('');
    } catch {
      toast.error('Failed to submit revision');
    } finally {
      setActionLoading(false);
    }
  };

  const handleSignUndertaking = async () => {
    setActionLoading(true);
    try {
      await dispatch(signDelayUndertaking({
        id: req.id,
        data: { notes: undertakingNotes.trim() || undefined },
      })).unwrap();
      toast.success('Delay undertaking signed successfully');
      setShowUndertakingModal(false);
      setUndertakingNotes('');
    } catch {
      toast.error('Failed to sign undertaking');
    } finally {
      setActionLoading(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto space-y-5 p-4">
      {/* Header */}
      <div className="flex items-center gap-3">
        <button onClick={() => navigate(-1)} className="text-gray-400 hover:text-gray-700">
          <ArrowLeft size={20} />
        </button>
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-bold font-mono text-gray-900">{req.requestId}</h1>
            <StatusBadge status={req.status} activeStepNames={req.activeWorkflowStepNames} />
          </div>
          <p className="text-sm text-gray-400">
            Submitted {format(new Date(req.submittedAt), 'dd MMM yyyy')}
          </p>
        </div>
      </div>

      {/* Workflow Stepper */}
      <WorkflowStepper currentStatus={req.status} requestType={req.requestType} />

      {/* Delay Undertaking – shown when customer action is needed */}
      {(req.status === 'PossessionLetterSigned' || req.status === 'DelayUndertakingRequested') && (
        <Card title="Delay Undertaking">
          <div className="space-y-4">
            {req.status === 'DelayUndertakingRequested' ? (
              <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 text-sm space-y-1">
                <p className="font-semibold text-amber-900">DDFC has requested your signature</p>
                {req.delayUndertaking?.delayReason && (
                  <p className="text-amber-700">
                    <span className="font-medium">Reason:</span> {req.delayUndertaking.delayReason}
                  </p>
                )}
                {req.delayUndertaking?.expectedDelayDays != null && (
                  <p className="text-amber-700">
                    <span className="font-medium">Expected delay:</span>{' '}
                    {req.delayUndertaking.expectedDelayDays} days
                  </p>
                )}
              </div>
            ) : (
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 text-sm">
                <p className="font-semibold text-blue-900">Optional: Sign a Delay Undertaking</p>
                <p className="text-blue-700 text-xs mt-1">
                  If you wish to formally acknowledge a delay in the design process, you can
                  sign a delay undertaking below.
                </p>
              </div>
            )}

            <Button
              variant="primary"
              className="w-full"
              onClick={() => setShowUndertakingModal(true)}
              icon={<FileSignature size={16} />}
            >
              Sign Delay Undertaking
            </Button>
          </div>
        </Card>
      )}

      {/* Plan Approval section */}
      {hasPlan && (
        <Card title="Architectural Plan Review">
          <div className="space-y-3">
            {(req.documents ?? [])
              .filter((d) => d.documentType.toLowerCase().includes('plan'))
              .map((doc) => (
                <div key={doc.documentId} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                  <div>
                    <p className="text-sm font-medium">{doc.documentType}</p>
                    <p className="text-xs text-gray-400">
                      {format(new Date(doc.uploadedAt), 'dd MMM yyyy')}
                    </p>
                  </div>
                  <a
                    href={doc.fileUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-center gap-1 text-sm text-blue-600 hover:underline"
                  >
                    <Download size={14} />
                    Download
                  </a>
                </div>
              ))}

            {canApprove && (
              <div className="flex gap-2 pt-2">
                <Button
                  variant="primary"
                  loading={actionLoading}
                  onClick={handleApprove}
                  icon={<CheckCircle size={14} />}
                >
                  Approve Plan
                </Button>
                <Button
                  variant="outline"
                  onClick={() => setShowRevisionModal(true)}
                  icon={<RotateCcw size={14} />}
                >
                  Request Revision
                </Button>
              </div>
            )}
          </div>
        </Card>
      )}

      {/* Payments */}
      {req.payments && req.payments.length > 0 && (
        <Card title="Payments & Challans">
          <div className="space-y-2">
            {req.payments.map((p) => (
              <div key={p.paymentId} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg text-sm">
                <div>
                  <p className="font-medium">Challan #{p.challanNumber}</p>
                  <p className="text-xs text-gray-400">PKR {p.amount.toLocaleString()}</p>
                </div>
                <div className="flex items-center gap-3">
                  <span className={`text-xs font-medium ${p.isPaid ? 'text-green-600' : 'text-amber-600'}`}>
                    {p.isPaid ? 'Paid' : 'Pending'}
                  </span>
                  {p.challanPdfUrl && (
                    <a href={p.challanPdfUrl} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline flex items-center gap-1">
                      <Download size={13} /> Challan
                    </a>
                  )}
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Timeline */}
      <Card title="Activity History">
        <WorkflowTimeline history={req.workflowHistory ?? []} />
      </Card>

      {/* Revision Modal */}
      <Modal
        isOpen={showRevisionModal}
        onClose={() => setShowRevisionModal(false)}
        title="Request Plan Revision"
        footer={
          <div className="flex gap-2 justify-end">
            <Button variant="ghost" onClick={() => setShowRevisionModal(false)}>Cancel</Button>
            <Button variant="primary" loading={actionLoading} onClick={handleRevision}>
              Submit
            </Button>
          </div>
        }
      >
        <Textarea
          label="Describe the changes needed"
          placeholder="e.g. Kitchen dimensions need to be altered..."
          value={revisionNote}
          onChange={(e) => setRevisionNote(e.target.value)}
          rows={5}
        />
      </Modal>

      {/* Delay Undertaking Signing Modal */}
      <Modal
        isOpen={showUndertakingModal}
        onClose={() => setShowUndertakingModal(false)}
        title="Sign Delay Undertaking"
        footer={
          <div className="flex gap-2 justify-end">
            <Button variant="ghost" onClick={() => setShowUndertakingModal(false)}>Cancel</Button>
            <Button
              variant="primary"
              loading={actionLoading}
              onClick={handleSignUndertaking}
              icon={<FileSignature size={14} />}
            >
              Confirm &amp; Sign
            </Button>
          </div>
        }
      >
        <div className="space-y-3">
          <p className="text-sm text-gray-600">
            By signing this undertaking, you formally acknowledge and accept the delay in the
            possession/design workflow process.
          </p>
          <Textarea
            label="Additional Notes (optional)"
            placeholder="Any comments about the delay acceptance…"
            value={undertakingNotes}
            onChange={(e) => setUndertakingNotes(e.target.value)}
            rows={3}
          />
        </div>
      </Modal>
    </div>
  );
};
