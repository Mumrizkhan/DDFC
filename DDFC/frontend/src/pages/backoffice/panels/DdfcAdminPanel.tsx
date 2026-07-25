import React, { useState } from 'react';
import { PenLine, FileText, Printer } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { ddfcAdminSign } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';
import api from '../../../services/api';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const DdfcAdminPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const [signing, setSigning] = useState(false);

  const [handedOverBy, setHandedOverBy]     = useState('');
  const [handedOverDate, setHandedOverDate] = useState('');
  const [takenOverBy, setTakenOverBy]       = useState('');
  const [takenOverDate, setTakenOverDate]   = useState('');
  const [chiefSurveyor, setChiefSurveyor]   = useState('');
  const [adTpBcd, setAdTpBcd]               = useState('');

  const handleSign = async () => {
    if (!handedOverBy.trim() || !takenOverBy.trim()) {
      toast.error('Handed-over-by and taken-over-by are required');
      return;
    }
    setSigning(true);
    try {
      await dispatch(ddfcAdminSign({
        id: requestId,
        handedOverBy:      handedOverBy.trim(),
        handedOverDate:    handedOverDate || undefined,
        takenOverBy:       takenOverBy.trim(),
        takenOverDate:     takenOverDate || undefined,
        chiefSurveyorName: chiefSurveyor.trim() || undefined,
        adTpBcdName:       adTpBcd.trim() || undefined,
      })).unwrap();
      toast.success('Possession letter signed successfully');
    } catch {
      toast.error('Failed to sign possession letter');
    } finally {
      setSigning(false);
    }
  };

  const handlePrint = () => {
    const url = `${api.defaults.baseURL}/requests/${requestId}/possession-certificate/preview`;
    const win = window.open(url, '_blank');
    if (!win) toast.warn('Popup blocked — please allow popups and try again');
  };

  const canSign = request.status === 'BothBranchesCleared' || request.status === 'PossessionIssued';

  return (
    <Card title="DDFC Admin – Sign Possession Letter">
      <div className="space-y-5">

        {/* Certificate summary */}
        <div className="p-4 bg-blue-50 border border-blue-200 rounded-lg">
          <div className="flex items-start gap-3">
            <FileText size={20} className="text-blue-600 mt-0.5 shrink-0" />
            <div className="text-sm text-blue-800 space-y-1">
              <p className="font-semibold">Possession Letter Ready for Signing</p>
              <p>
                <span className="font-medium">Owner:</span>{' '}
                {request.ownerName ?? '—'}
              </p>
              <p>
                <span className="font-medium">Plot:</span>{' '}
                {request.plotNumber} — Sector {request.sectorNo}, Phase {request.phaseNo}
              </p>
            </div>
          </div>
        </div>

        {canSign ? (
          <>
            {/* Handover Details */}
            <div>
              <h4 className="text-sm font-semibold text-gray-700 mb-2">Handover Details</h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <Input
                  label="Possession Handed Over By *"
                  placeholder="Name of handing-over officer"
                  value={handedOverBy}
                  onChange={(e) => setHandedOverBy(e.target.value)}
                />
                <Input
                  label="Handover Date"
                  type="date"
                  value={handedOverDate}
                  onChange={(e) => setHandedOverDate(e.target.value)}
                />
                <Input
                  label="Taken Over By *"
                  placeholder="Name of receiving officer"
                  value={takenOverBy}
                  onChange={(e) => setTakenOverBy(e.target.value)}
                />
                <Input
                  label="Takeover Date"
                  type="date"
                  value={takenOverDate}
                  onChange={(e) => setTakenOverDate(e.target.value)}
                />
                <Input
                  label="Chief Surveyor"
                  placeholder="Chief Surveyor name (optional)"
                  value={chiefSurveyor}
                  onChange={(e) => setChiefSurveyor(e.target.value)}
                />
                <Input
                  label="AD TP/BCD"
                  placeholder="AD TP/BCD name (optional)"
                  value={adTpBcd}
                  onChange={(e) => setAdTpBcd(e.target.value)}
                />
              </div>
            </div>

            {/* Preview before signing */}
            <Button
              variant="secondary"
              className="w-full"
              onClick={handlePrint}
              icon={<Printer size={16} />}
            >
              Preview / Print Possession Letter
            </Button>

            <div className="space-y-2">
              <p className="text-xs text-gray-500">
                Review the possession letter above, then click to confirm your signature and advance
                the request to package selection.
              </p>
              <Button
                variant="primary"
                className="w-full"
                loading={signing}
                onClick={handleSign}
                icon={<PenLine size={16} />}
              >
                Sign &amp; Approve Possession Letter
              </Button>
            </div>
          </>
        ) : (
          <>
            {/* Preview after signing */}
            <Button
              variant="secondary"
              className="w-full"
              onClick={handlePrint}
              icon={<Printer size={16} />}
            >
              Preview / Print Possession Letter
            </Button>

            <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-sm text-green-800 flex items-center gap-2">
              <PenLine size={14} />
              Possession letter has already been signed.
            </div>
          </>
        )}
      </div>
    </Card>
  );
};
