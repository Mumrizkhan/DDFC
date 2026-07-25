import React, { useState } from 'react';
import { CheckCircle, XCircle } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import { Textarea, Input } from '../../../components/ui/Input';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch, useAppSelector } from '../../../store/hooks';
import { adminReview } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

const isGuardianCase = (rel?: string) => !!rel && rel !== 'Self';

export const AdminReviewPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const { actionLoading } = useAppSelector((s) => s.requests);

  const guardianCase = isGuardianCase(request.guardianRelation);

  // Required docs
  const [allotmentLetterUrl, setAllotmentLetterUrl] = useState(request.allotmentLetterUrl ?? '');
  const [cnicUrl, setCnicUrl] = useState(request.cnicUrl ?? '');

  // Guardian-only docs
  const [msgScreenshotUrl, setMsgScreenshotUrl] = useState(request.messageScreenshotUrl ?? '');
  const [eStampPaperUrl, setEStampPaperUrl] = useState(request.eStampPaperUrl ?? '');
  const [authCnicUrl, setAuthCnicUrl] = useState(request.authorizedPersonCnicUrl ?? '');
  const [authPhone, setAuthPhone] = useState(request.authorizedPersonPhone ?? '');

  // Rejection
  const [rejectionReason, setRejectionReason] = useState('');
  const [showRejectForm, setShowRejectForm] = useState(false);

  const validate = (action: 'Initiate' | 'Reject') => {
    if (!allotmentLetterUrl) { toast.error('Allotment Letter is required'); return false; }
    if (!cnicUrl) { toast.error('CNIC is required'); return false; }
    if (guardianCase) {
      if (!msgScreenshotUrl) { toast.error('Message screenshot of allottee is required'); return false; }
      if (!eStampPaperUrl) { toast.error('E-Stamp Paper is required'); return false; }
      if (!authCnicUrl) { toast.error('Authorized person CNIC is required'); return false; }
      if (!authPhone.trim()) { toast.error('Authorized person phone is required'); return false; }
    }
    if (action === 'Reject' && !rejectionReason.trim()) {
      toast.error('Rejection reason is required');
      return false;
    }
    return true;
  };

  const submit = async (action: 'Initiate' | 'Reject') => {
    if (!validate(action)) return;
    try {
      await dispatch(
        adminReview({
          id: requestId,
          data: {
            action,
            rejectionReason: action === 'Reject' ? rejectionReason : undefined,
            allotmentLetterUrl,
            cnicUrl,
            messageScreenshotUrl: guardianCase ? msgScreenshotUrl : undefined,
            eStampPaperUrl: guardianCase ? eStampPaperUrl : undefined,
            authorizedPersonCnicUrl: guardianCase ? authCnicUrl : undefined,
            authorizedPersonPhone: guardianCase ? authPhone.trim() : undefined,
          },
        })
      ).unwrap();
      toast.success(action === 'Initiate' ? 'Request initiated — workflow started' : 'Request rejected');
      setShowRejectForm(false);
    } catch {
      toast.error('Action failed');
    }
  };

  return (
    <Card title="Admin Review">
      <div className="space-y-5">
        {/* Required Documents */}
        <div>
          <h4 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-3">
            Required Documents
          </h4>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FileUploadButton
              label="Allotment Letter"
              required
              value={allotmentLetterUrl}
              onUploaded={setAllotmentLetterUrl}
            />
            <FileUploadButton
              label="CNIC (Allottee)"
              required
              value={cnicUrl}
              onUploaded={setCnicUrl}
            />
          </div>
        </div>

        {/* Guardian-specific Documents */}
        {guardianCase && (
          <div>
            <h4 className="text-sm font-semibold text-amber-700 uppercase tracking-wide mb-1">
              Guardian / Authorized Person Documents
            </h4>
            <p className="text-xs text-amber-600 mb-3">
              Required because request is initiated by a guardian (relation: <strong>{request.guardianRelation}</strong>)
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FileUploadButton
                label="Message Screenshot (Allottee)"
                required
                value={msgScreenshotUrl}
                onUploaded={setMsgScreenshotUrl}
              />
              <FileUploadButton
                label="E-Stamp Paper"
                required
                value={eStampPaperUrl}
                onUploaded={setEStampPaperUrl}
              />
              <FileUploadButton
                label="Authorized Person — CNIC"
                required
                value={authCnicUrl}
                onUploaded={setAuthCnicUrl}
              />
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Authorized Person — Phone <span className="text-red-500">*</span>
                </label>
                <Input
                  value={authPhone}
                  onChange={(e) => setAuthPhone(e.target.value)}
                  placeholder="e.g., 03001234567"
                />
              </div>
            </div>
          </div>
        )}

        {/* Rejection Form */}
        {showRejectForm && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Rejection Reason <span className="text-red-500">*</span>
            </label>
            <Textarea
              value={rejectionReason}
              onChange={(e) => setRejectionReason(e.target.value)}
              placeholder="Provide a clear reason for rejection…"
              rows={3}
            />
          </div>
        )}

        {/* Actions */}
        <div className="flex flex-wrap gap-3 pt-2 border-t">
          <Button
            variant="primary"
            loading={actionLoading}
            onClick={() => submit('Initiate')}
            icon={<CheckCircle size={16} />}
          >
            Initiate Request
          </Button>

          {!showRejectForm ? (
            <Button
              variant="danger"
              onClick={() => setShowRejectForm(true)}
              icon={<XCircle size={16} />}
            >
              Reject
            </Button>
          ) : (
            <>
              <Button
                variant="danger"
                loading={actionLoading}
                onClick={() => submit('Reject')}
                icon={<XCircle size={16} />}
              >
                Confirm Reject
              </Button>
              <Button variant="secondary" onClick={() => { setShowRejectForm(false); setRejectionReason(''); }}>
                Cancel
              </Button>
            </>
          )}
        </div>
      </div>
    </Card>
  );
};
