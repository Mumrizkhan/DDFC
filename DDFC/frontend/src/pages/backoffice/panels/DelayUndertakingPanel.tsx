import React, { useState } from 'react';
import { FileSignature, Send, SkipForward } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Input, Textarea } from '../../../components/ui/Input';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch, useAppSelector } from '../../../store/hooks';
import { requestDelayUndertaking, skipDelayUndertaking } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const DelayUndertakingPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const { actionLoading } = useAppSelector((s) => s.requests);

  const [delayReason, setDelayReason] = useState('');
  const [expectedDelayDays, setExpectedDelayDays] = useState('');
  const [notes, setNotes] = useState('');

  const undertaking = request.delayUndertaking;
  const isRequested = request.status === 'DelayUndertakingRequested';
  const isSigned    = request.status === 'DelayUndertakingSigned';

  const handleRequest = async () => {
    if (!delayReason.trim()) {
      toast.error('Please provide a reason for the delay');
      return;
    }
    try {
      await dispatch(requestDelayUndertaking({
        id: requestId,
        data: {
          delayReason: delayReason.trim(),
          expectedDelayDays: expectedDelayDays ? parseInt(expectedDelayDays, 10) : undefined,
          notes: notes.trim() || undefined,
        },
      })).unwrap();
      toast.success('Delay undertaking requested — customer will be notified to sign');
    } catch {
      toast.error('Failed to request delay undertaking');
    }
  };

  const handleSkip = async () => {
    try {
      await dispatch(skipDelayUndertaking(requestId)).unwrap();
      toast.success('Delay undertaking skipped — proceeding to package selection');
    } catch {
      toast.error('Failed to skip delay undertaking');
    }
  };

  return (
    <Card title="Delay Undertaking Signing">
      <div className="space-y-5">

        {/* Context info */}
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 text-sm space-y-1">
          <p className="font-semibold text-amber-900">
            Possession letter has been signed.
          </p>
          <p className="text-amber-700 text-xs">
            A delay undertaking can be requested from the customer if needed, or you can skip
            this step and proceed directly to package selection.
          </p>
        </div>

        {/* Already signed */}
        {isSigned && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-4 space-y-2">
            <div className="flex items-center gap-2 text-green-800 font-semibold">
              <FileSignature className="w-5 h-5" />
              {undertaking ? 'Delay Undertaking Signed' : 'Delay Undertaking Skipped'}
            </div>
            {undertaking ? (
              <div className="text-sm text-green-700 space-y-1">
                <p>
                  <span className="font-medium">Requested by:</span>{' '}
                  {undertaking.initiatedBy === 'DDFC' ? 'DDFC' : 'Customer'}
                </p>
                {undertaking.delayReason && (
                  <p><span className="font-medium">Reason:</span> {undertaking.delayReason}</p>
                )}
                {undertaking.expectedDelayDays != null && (
                  <p><span className="font-medium">Expected delay:</span> {undertaking.expectedDelayDays} days</p>
                )}
                {undertaking.signedAt && (
                  <p><span className="font-medium">Signed at:</span> {new Date(undertaking.signedAt).toLocaleString()}</p>
                )}
                {undertaking.undertakingDocumentUrl && (
                  <a
                    href={undertaking.undertakingDocumentUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-blue-600 underline text-xs"
                  >
                    View Signed Document
                  </a>
                )}
              </div>
            ) : (
              <p className="text-sm text-green-700">
                This step was skipped by DDFC staff. The workflow is proceeding to package selection.
              </p>
            )}
          </div>
        )}

        {/* Waiting for customer to sign (DDFC already requested) */}
        {isRequested && (
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 space-y-2">
            <p className="text-sm font-semibold text-blue-800">Awaiting Customer Signature</p>
            {undertaking?.delayReason && (
              <p className="text-sm text-blue-700"><span className="font-medium">Reason sent:</span> {undertaking.delayReason}</p>
            )}
            {undertaking?.expectedDelayDays != null && (
              <p className="text-sm text-blue-700"><span className="font-medium">Expected delay:</span> {undertaking.expectedDelayDays} days</p>
            )}
            <p className="text-xs text-blue-600">The customer can sign from their portal.</p>

            {/* Still allow skipping even after DDFC requested */}
            <Button
              variant="outline"
              className="w-full mt-2"
              loading={actionLoading}
              onClick={handleSkip}
              icon={<SkipForward className="w-4 h-4" />}
            >
              Skip — Proceed Without Signature
            </Button>
          </div>
        )}

        {/* Actions when status = PossessionLetterSigned */}
        {request.status === 'PossessionLetterSigned' && (
          <div className="space-y-4">

            {/* Request undertaking form */}
            <div className="space-y-3">
              <p className="text-sm font-medium text-gray-700">Request Customer to Sign Delay Undertaking</p>

              <Textarea
                label="Reason for Delay *"
                value={delayReason}
                onChange={(e) => setDelayReason(e.target.value)}
                placeholder="e.g. Design review backlog, material procurement delays…"
                rows={3}
              />

              <Input
                label="Expected Delay (days)"
                type="number"
                min={1}
                value={expectedDelayDays}
                onChange={(e) => setExpectedDelayDays(e.target.value)}
                placeholder="e.g. 30"
              />

              <Textarea
                label="Additional Notes"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Any additional context for the customer…"
                rows={2}
              />

              <Button
                variant="primary"
                className="w-full"
                loading={actionLoading}
                onClick={handleRequest}
                icon={<Send className="w-4 h-4" />}
              >
                Request Customer Signature
              </Button>
            </div>

            {/* Divider */}
            <div className="flex items-center gap-3">
              <div className="flex-1 border-t border-gray-200" />
              <span className="text-xs text-gray-400 uppercase tracking-wide">or</span>
              <div className="flex-1 border-t border-gray-200" />
            </div>

            {/* Skip button */}
            <Button
              variant="outline"
              className="w-full text-gray-600"
              loading={actionLoading}
              onClick={handleSkip}
              icon={<SkipForward className="w-4 h-4" />}
            >
              Skip — Proceed to Package Selection
            </Button>
          </div>
        )}
      </div>
    </Card>
  );
};
