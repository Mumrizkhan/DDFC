import React, { useState } from 'react';
import { FileText, Printer, Mail, UploadCloud, Clock, CheckCircle } from 'lucide-react';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { Input, Textarea } from '../ui/Input';
import { FileUploadButton } from '../ui/FileUploadButton';
import { toast } from 'react-toastify';
import api from '../../services/api';
import { useAppDispatch } from '../../store/hooks';
import { attachSignedUndertaking } from '../../store/slices/requestsSlice';
import type { PossessionRequest } from '../../types';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  request: PossessionRequest;
}

export const UndertakingModal: React.FC<Props> = ({ isOpen, onClose, request }) => {
  const dispatch = useAppDispatch();

  const [sendingEmail, setSendingEmail] = useState(false);
  const [signedDocUrl, setSignedDocUrl] = useState('');
  const [holdDays, setHoldDays] = useState('');
  const [notes, setNotes] = useState('');
  const [attaching, setAttaching] = useState(false);

  const existing = request.architectUndertaking;

  const handlePreview = async () => {
    try {
      const res = await api.get<string>(`/requests/${request.id}/undertaking/preview`, {
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
      toast.error('Failed to load undertaking preview');
    }
  };

  const handleSendEmail = async () => {
    setSendingEmail(true);
    try {
      await api.post(`/requests/${request.id}/undertaking/send-email`);
      toast.success('Undertaking sent to customer via email');
    } catch {
      toast.error('Failed to send email — please try again');
    } finally {
      setSendingEmail(false);
    }
  };

  const handleAttach = async () => {
    if (!signedDocUrl) {
      toast.error('Please upload the signed undertaking document');
      return;
    }
    const days = parseInt(holdDays, 10);
    if (!holdDays || isNaN(days) || days < 1) {
      toast.error('Please enter a valid hold duration (minimum 1 day)');
      return;
    }
    setAttaching(true);
    try {
      await dispatch(
        attachSignedUndertaking({
          id: request.id,
          data: {
            signedDocumentUrl: signedDocUrl,
            holdDays: days,
            notes: notes.trim() || undefined,
          },
        })
      ).unwrap();
      toast.success('Signed undertaking attached — request is now on hold');
      onClose();
    } catch {
      toast.error('Failed to attach undertaking');
    } finally {
      setAttaching(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Undertaking" size="lg">
      <div className="space-y-6">

        {/* Already attached banner */}
        {existing?.signedDocumentUrl && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-3 text-sm space-y-1">
            <div className="flex items-center gap-2 text-green-800 font-semibold">
              <CheckCircle size={14} />
              Signed undertaking already on file
            </div>
            {existing.holdDays != null && (
              <p className="text-green-700">
                Hold duration: <span className="font-medium">{existing.holdDays} days</span>
              </p>
            )}
            {existing.holdEndDate && (
              <p className="text-green-700">
                On hold until:{' '}
                <span className="font-medium">
                  {new Date(existing.holdEndDate).toLocaleDateString()}
                </span>
              </p>
            )}
            <a
              href={existing.signedDocumentUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-green-700 underline text-xs"
            >
              View signed document
            </a>
          </div>
        )}

        {/* ── Section 1: Generate & distribute ─────────────────────────── */}
        <div className="space-y-3">
          <h3 className="text-sm font-semibold text-gray-700 flex items-center gap-2">
            <FileText size={14} />
            Undertaking Document
          </h3>
          <p className="text-xs text-gray-500">
            Generate an official undertaking document for this request. Preview or print it, or
            send it directly to the customer's registered email address.
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              variant="outline"
              size="sm"
              icon={<Printer size={14} />}
              onClick={handlePreview}
            >
              Preview / Print
            </Button>
            <Button
              variant="secondary"
              size="sm"
              icon={<Mail size={14} />}
              loading={sendingEmail}
              onClick={handleSendEmail}
            >
              Send to Customer via Email
            </Button>
          </div>
        </div>

        <div className="border-t border-gray-100" />

        {/* ── Section 2: Attach signed doc + hold ──────────────────────── */}
        <div className="space-y-3">
          <h3 className="text-sm font-semibold text-gray-700 flex items-center gap-2">
            <UploadCloud size={14} />
            Attach Signed Undertaking & Hold Request
          </h3>
          <p className="text-xs text-gray-500">
            Once the customer has signed the undertaking, upload the signed document and enter the
            agreed hold duration. The request will be placed on hold for that many days.
          </p>

          <FileUploadButton
            label="Signed Undertaking Document"
            required
            accept=".pdf,.jpg,.jpeg,.png"
            value={signedDocUrl}
            onUploaded={setSignedDocUrl}
          />

          <Input
            label="Hold Duration (days)"
            type="number"
            min={1}
            placeholder="e.g. 30"
            value={holdDays}
            onChange={(e) => setHoldDays(e.target.value)}
          />

          <Textarea
            label="Notes (optional)"
            placeholder="Any additional notes about the undertaking…"
            rows={2}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />

          <Button
            variant="primary"
            loading={attaching}
            disabled={!signedDocUrl || !holdDays}
            icon={<Clock size={14} />}
            onClick={handleAttach}
          >
            Attach & Place Request on Hold
          </Button>
        </div>
      </div>
    </Modal>
  );
};
