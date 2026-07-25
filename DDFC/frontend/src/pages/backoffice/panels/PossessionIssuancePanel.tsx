import React, { useState } from 'react';
import { FileText, Printer, CheckCircle } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { issuePossessionCert } from '../../../store/slices/requestsSlice';
import api from '../../../services/api';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const PossessionIssuancePanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();

  const [handedOverBy, setHandedOverBy]       = useState('');
  const [handedOverDate, setHandedOverDate]   = useState('');
  const [takenOverBy, setTakenOverBy]         = useState('');
  const [takenOverDate, setTakenOverDate]     = useState('');
  const [chiefSurveyor, setChiefSurveyor]     = useState('');
  const [adTpBcd, setAdTpBcd]                 = useState('');
  const [submitting, setSubmitting]           = useState(false);

  const isAlreadyIssued = request.status === 'PossessionIssued';

  const handleIssueCert = async () => {
    if (!handedOverBy.trim() || !takenOverBy.trim()) {
      toast.error('Handed-over-by and taken-over-by are required');
      return;
    }
    setSubmitting(true);
    try {
      await dispatch(issuePossessionCert({
        id: requestId,
        handedOverBy:    handedOverBy.trim(),
        handedOverDate:  handedOverDate || undefined,
        takenOverBy:     takenOverBy.trim(),
        takenOverDate:   takenOverDate || undefined,
        chiefSurveyorName: chiefSurveyor.trim() || undefined,
        adTpBcdName:     adTpBcd.trim() || undefined,
      })).unwrap();
      toast.success('Possession certificate issued');
    } catch {
      toast.error('Failed to issue possession certificate');
    } finally {
      setSubmitting(false);
    }
  };

  const handlePrint = async () => {
    try {
      const res = await api.get<string>(`/requests/${requestId}/possession-certificate/preview`, {
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
      toast.error('Failed to load certificate preview');
    }
  };

  return (
    <Card title="Town Planning – Possession Certificate">
      <div className="space-y-5">

        {/* Plot summary */}
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 text-sm space-y-1">
          <p className="font-semibold text-amber-900">Plot: {request.plotNumber ?? '—'} | Sector: {request.sectorNo ?? '—'} | Phase: {request.phaseNo ?? '—'}</p>
          <p className="text-amber-700">Owner: {request.ownerTitle} {request.ownerName} | CNIC: {request.cnic ?? '—'}</p>
          <div className="flex gap-2 mt-1">
            {request.transferApproved && (
              <span className="inline-flex items-center gap-1 text-green-700 font-medium text-xs">
                <CheckCircle className="w-3 h-3" /> Transfer Approved
              </span>
            )}
            {request.financeApproved && (
              <span className="inline-flex items-center gap-1 text-green-700 font-medium text-xs">
                <CheckCircle className="w-3 h-3" /> Finance Approved
              </span>
            )}
          </div>
        </div>

        {isAlreadyIssued ? (
          <div className="bg-green-50 border border-green-200 rounded-lg p-4 text-center space-y-3">
            <p className="text-green-800 font-semibold text-base">
              ✅ Possession Certificate has been issued
            </p>
            <Button variant="primary" onClick={handlePrint} icon={<Printer className="w-4 h-4" />}>
              Print / View Certificate
            </Button>
          </div>
        ) : (
          <>
            <p className="text-sm text-gray-600">
              Fill in the handover details and stamp information, then issue and print the
              possession certificate. Both Transfer and Finance must be approved.
            </p>

            {/* Handover section */}
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
                  label="Possession Taken Over By *"
                  placeholder="Owner / representative name"
                  value={takenOverBy}
                  onChange={(e) => setTakenOverBy(e.target.value)}
                />
                <Input
                  label="Takeover Date"
                  type="date"
                  value={takenOverDate}
                  onChange={(e) => setTakenOverDate(e.target.value)}
                />
              </div>
            </div>

            {/* Stamps section */}
            <div>
              <h4 className="text-sm font-semibold text-gray-700 mb-2">Official Stamps</h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <Input
                  label="Chief Surveyor Name"
                  placeholder="e.g. Col. (R) Muhammad Ali"
                  value={chiefSurveyor}
                  onChange={(e) => setChiefSurveyor(e.target.value)}
                />
                <Input
                  label="AD TP & BCD Name"
                  placeholder="e.g. Lt. Col. (R) Ahmed Khan"
                  value={adTpBcd}
                  onChange={(e) => setAdTpBcd(e.target.value)}
                />
              </div>
            </div>

            <div className="flex flex-col sm:flex-row gap-3 pt-2">
              <Button
                variant="primary"
                onClick={handleIssueCert}
                disabled={submitting || !handedOverBy.trim() || !takenOverBy.trim()}
                icon={<FileText className="w-4 h-4" />}
              >
                {submitting ? 'Issuing…' : 'Issue & Save Certificate'}
              </Button>
              <Button
                variant="secondary"
                onClick={handlePrint}
                icon={<Printer className="w-4 h-4" />}
              >
                Preview Certificate (Draft)
              </Button>
            </div>
          </>
        )}
      </div>
    </Card>
  );
};
