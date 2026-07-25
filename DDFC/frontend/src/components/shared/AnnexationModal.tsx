import React, { useState } from 'react';
import { Maximize2, CheckCircle, ExternalLink } from 'lucide-react';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { Input, Textarea } from '../ui/Input';
import { toast } from 'react-toastify';
import { useAppDispatch } from '../../store/hooks';
import { requestAnnexation } from '../../store/slices/requestsSlice';
import type { PossessionRequest } from '../../types';

const SETTINGS_KEY = 'ddfc_system_settings';

function getDefaultAnnexationFee(): number {
  try {
    const raw = localStorage.getItem(SETTINGS_KEY);
    const settings: Record<string, string> = raw ? JSON.parse(raw) : {};
    const v = parseInt(settings['PlotModification_AnnexationFee'] ?? '15000', 10);
    return isNaN(v) || v < 0 ? 15000 : v;
  } catch {
    return 15000;
  }
}

interface Props {
  isOpen: boolean;
  onClose: () => void;
  request: PossessionRequest;
}

export const AnnexationModal: React.FC<Props> = ({ isOpen, onClose, request }) => {
  const dispatch = useAppDispatch();

  const [additionalArea, setAdditionalArea] = useState('');
  const [annexationFee, setAnnexationFee] = useState(() => String(getDefaultAnnexationFee()));
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const existing = request.plotAnnexation;

  // Reset form when modal opens
  React.useEffect(() => {
    if (isOpen) {
      setAdditionalArea('');
      setAnnexationFee(String(getDefaultAnnexationFee()));
      setNotes('');
    }
  }, [isOpen]);

  const handleSubmit = async () => {
    const fee = parseFloat(annexationFee);
    if (isNaN(fee) || fee < 0) {
      toast.error('Please enter a valid annexation fee');
      return;
    }
    setSubmitting(true);
    try {
      await dispatch(
        requestAnnexation({
          id: request.id,
          data: {
            additionalArea: additionalArea.trim() || undefined,
            annexationFee: fee,
            notes: notes.trim() || undefined,
          },
        })
      ).unwrap();
      toast.success('Plot annexation recorded successfully');
      onClose();
    } catch {
      toast.error('Failed to record annexation');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Plot Annexation" size="md">
      <div className="space-y-5">

        {/* Info banner */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 text-sm text-blue-800 space-y-1">
          <p className="font-semibold flex items-center gap-1.5">
            <Maximize2 size={14} />
            What is Annexation?
          </p>
          <p className="text-blue-700 text-xs">
            Annexation adds adjacent extra land to the existing plot. An additional fee applies
            and is charged on top of the regular package fee.
          </p>
        </div>

        {/* Already recorded */}
        {existing && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-3 text-sm space-y-1">
            <div className="flex items-center gap-2 text-green-800 font-semibold">
              <CheckCircle size={14} />
              Annexation already recorded
            </div>
            {existing.additionalArea && (
              <p className="text-green-700">Additional area: <span className="font-medium">{existing.additionalArea}</span></p>
            )}
            <p className="text-green-700">
              Fee: <span className="font-medium">PKR {existing.annexationFee.toLocaleString()}</span>
            </p>
            {existing.notes && (
              <p className="text-green-700 text-xs">Notes: {existing.notes}</p>
            )}
            {existing.documentUrl && (
              <a href={existing.documentUrl} target="_blank" rel="noopener noreferrer"
                className="inline-flex items-center gap-1 text-green-700 underline text-xs">
                <ExternalLink size={11} /> View document
              </a>
            )}
          </div>
        )}

        {/* Form */}
        <Input
          label="Additional Area"
          placeholder="e.g. 2 Marla, 200 sq. ft."
          value={additionalArea}
          onChange={(e) => setAdditionalArea(e.target.value)}
          hint="Describe the area being annexed (optional)"
        />

        <Input
          label="Annexation Fee (PKR)"
          type="number"
          min={0}
          step={500}
          value={annexationFee}
          onChange={(e) => setAnnexationFee(e.target.value)}
          hint="Default from System Settings. Adjust if needed."
        />

        <Textarea
          label="Notes (optional)"
          placeholder="Any relevant details about the annexation…"
          rows={2}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
        />

        <div className="flex gap-2 justify-end pt-1">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button
            variant="primary"
            loading={submitting}
            icon={<Maximize2 size={14} />}
            onClick={handleSubmit}
          >
            {existing ? 'Update Annexation' : 'Record Annexation'}
          </Button>
        </div>
      </div>
    </Modal>
  );
};
