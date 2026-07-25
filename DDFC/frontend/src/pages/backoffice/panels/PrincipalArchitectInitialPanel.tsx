import React, { useEffect, useState } from 'react';
import { UserCheck, UploadCloud, FileCheck, CheckCircle, FileSignature } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Input, Select } from '../../../components/ui/Input';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import { UndertakingModal } from '../../../components/shared/UndertakingModal';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { paInitialReview } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';
import api from '../../../services/api';

interface User { id: string; fullName: string; email: string; }

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const PrincipalArchitectInitialPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();

  const [architects,         setArchitects]         = useState<User[]>([]);
  const [assignedArchitectId, setAssignedArchitectId] = useState('');
  const [soilTestFileUrl,    setSoilTestFileUrl]     = useState('');
  const [soilTestDate,       setSoilTestDate]        = useState('');
  const [labName,            setLabName]             = useState('');
  const [bearingCapacity,    setBearingCapacity]     = useState('');
  const [resultSummary,      setResultSummary]       = useState('');
  const [notes,              setNotes]               = useState('');
  const [submitting,         setSubmitting]          = useState(false);
  const [showUndertaking,    setShowUndertaking]     = useState(false);

  const isComplete = request.status !== 'PackagePaid';

  // Load architects from the Architecture department
  useEffect(() => {
    api.get<User[]>('/admin/users', { params: { departmentCode: 'AD' } })
      .then((r) => setArchitects(r.data))
      .catch(() => {
        // fallback: load all users and filter client-side
        api.get<{ users: User[] }>('/admin/users')
          .then((r) => setArchitects(r.data.users ?? []))
          .catch(() => {});
      });
  }, []);

  const handleSubmit = async () => {
    if (!assignedArchitectId) { toast.error('Please select an architect'); return; }
    setSubmitting(true);
    try {
      await dispatch(paInitialReview({
        id: requestId,
        data: {
          assignedArchitectId,
          soilTestFileUrl:     soilTestFileUrl  || undefined,
          soilTestDate:        soilTestDate     || undefined,
          labName:             labName.trim()   || undefined,
          soilBearingCapacity: bearingCapacity.trim() || undefined,
          resultSummary:       resultSummary.trim()   || undefined,
          notes:               notes.trim()     || undefined,
        },
      })).unwrap();
      toast.success('Architect assigned — request advanced to Architecture step');
    } catch (err) {
      toast.error((err as string) || 'Failed to complete initial review');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="Principal Architect – Initial Review">
      <div className="space-y-5">

        {/* Undertaking button */}
        <div className="flex justify-end">
          <Button
            variant="outline"
            size="sm"
            icon={<FileSignature size={14} />}
            onClick={() => setShowUndertaking(true)}
          >
            Undertaking
          </Button>
        </div>

        {/* Info banner */}
        <div className="p-3 bg-violet-50 border border-violet-200 rounded-lg text-sm text-violet-800 flex items-start gap-2">
          <UserCheck size={16} className="mt-0.5 shrink-0" />
          <span>
            Review the request, optionally upload the soil test report, then assign an architect
            to advance the request to the Architecture design step.
          </span>
        </div>

        {/* Request summary */}
        <div className="bg-gray-50 rounded-lg p-3 text-xs grid grid-cols-2 gap-x-4 gap-y-1.5">
          {[
            ['Owner',     `${request.ownerTitle ?? ''} ${request.ownerName ?? '—'}`.trim()],
            ['Plot',      `${request.plotNumber ?? '—'} | Sector ${request.sectorNo ?? '—'}`],
            ['Plot Type', request.plotType ?? '—'],
            ['Plot Size', request.plotSize ?? '—'],
            ['Package',   request.packageTier ?? '—'],
          ].map(([k, v]) => (
            <React.Fragment key={k}>
              <span className="text-gray-400">{k}</span>
              <span className="font-medium text-gray-700">{v}</span>
            </React.Fragment>
          ))}
        </div>

        {!isComplete ? (
          <>
            {/* Architect assignment */}
            <div>
              <h4 className="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-1.5">
                <UserCheck size={14} />
                Assign Architect <span className="text-red-500">*</span>
              </h4>
              {architects.length === 0 ? (
                <p className="text-xs text-gray-400">Loading architects…</p>
              ) : (
                <Select
                  value={assignedArchitectId}
                  onChange={(e) => setAssignedArchitectId(e.target.value)}
                  options={[
                    { value: '', label: '— Select architect —' },
                    ...architects.map((a) => ({ value: a.id, label: a.fullName })),
                  ]}
                />
              )}
            </div>

            {/* Soil test (optional) */}
            <div>
              <h4 className="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-1.5">
                <UploadCloud size={14} />
                Soil Test Report <span className="text-xs text-gray-400 font-normal">(optional)</span>
              </h4>

              {request.soilTestReport ? (
                <div className="flex items-center gap-2 text-xs text-green-700 bg-green-50 rounded p-2">
                  <FileCheck size={13} />
                  Soil test already uploaded.{' '}
                  <a href={request.soilTestReport.reportUrl} target="_blank" rel="noopener noreferrer"
                     className="underline">View</a>
                </div>
              ) : (
                <div className="space-y-3">
                  <FileUploadButton
                    label="Soil Test Report (PDF)"
                    accept=".pdf,.jpg,.jpeg,.png"
                    value={soilTestFileUrl}
                    onUploaded={setSoilTestFileUrl}
                  />
                  {soilTestFileUrl && (
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                      <Input label="Test Date" type="date" value={soilTestDate}
                        onChange={(e) => setSoilTestDate(e.target.value)} />
                      <Input label="Lab Name" placeholder="Name of testing lab"
                        value={labName} onChange={(e) => setLabName(e.target.value)} />
                      <Input label="Soil Bearing Capacity (kN/m²)" placeholder="e.g. 150"
                        value={bearingCapacity} onChange={(e) => setBearingCapacity(e.target.value)} />
                      <Input label="Result Summary" placeholder="e.g. Suitable for construction"
                        value={resultSummary} onChange={(e) => setResultSummary(e.target.value)} />
                    </div>
                  )}
                </div>
              )}
            </div>

            <Input label="Notes (optional)" placeholder="Any remarks for the architect…"
              value={notes} onChange={(e) => setNotes(e.target.value)} />

            <Button
              variant="primary"
              className="w-full"
              loading={submitting}
              disabled={!assignedArchitectId}
              onClick={handleSubmit}
              icon={<CheckCircle size={14} />}
            >
              Assign Architect &amp; Advance to Architecture
            </Button>
          </>
        ) : (
          <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-sm text-green-800 flex items-center gap-2">
            <CheckCircle size={14} />
            Initial review complete. Assigned architect: <strong>{request.assignedArchitectName ?? '—'}</strong>
          </div>
        )}
      </div>

      <UndertakingModal
        isOpen={showUndertaking}
        onClose={() => setShowUndertaking(false)}
        request={request}
      />
    </Card>
  );
};
