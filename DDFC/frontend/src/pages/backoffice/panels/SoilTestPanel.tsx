import React, { useState } from 'react';
import { UploadCloud, FileCheck, CheckCircle } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { submitInitialSoilTest } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const SoilTestPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();

  const [soilTestFileUrl, setSoilTestFileUrl] = useState('');
  const [soilTestDate,    setSoilTestDate]    = useState('');
  const [labName,         setLabName]         = useState('');
  const [bearingCapacity, setBearingCapacity] = useState('');
  const [resultSummary,   setResultSummary]   = useState('');
  const [submitting,      setSubmitting]      = useState(false);

  const isComplete = request.status !== 'PackagePaid';

  const handleSubmit = async () => {
    if (!soilTestFileUrl) { toast.error('Please upload the soil test report'); return; }
    if (!soilTestDate.trim() || !labName.trim() || !bearingCapacity.trim() || !resultSummary.trim()) {
      toast.error('Please fill in all soil test fields');
      return;
    }
    setSubmitting(true);
    try {
      await dispatch(submitInitialSoilTest({
        id: requestId,
        data: {
          testDate:            soilTestDate,
          labName:             labName.trim(),
          soilBearingCapacity: bearingCapacity.trim(),
          resultSummary:       resultSummary.trim(),
          reportFileUrl:       soilTestFileUrl,
        },
      })).unwrap();
      toast.success('Soil test submitted — request advanced to Principal Architect Initial Review');
    } catch (err) {
      toast.error((err as string) || 'Failed to submit soil test');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="Soil Test">
      <div className="space-y-5">
        {/* Info banner */}
        <div className="p-3 bg-violet-50 border border-violet-200 rounded-lg text-sm text-violet-800 flex items-start gap-2">
          <UploadCloud size={16} className="mt-0.5 shrink-0" />
          <span>Upload the soil test report before the request moves to Principal Architect Initial Review.</span>
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

            <Button
              variant="primary"
              className="w-full"
              loading={submitting}
              disabled={!soilTestFileUrl}
              onClick={handleSubmit}
              icon={<CheckCircle size={14} />}
            >
              Submit Soil Test &amp; Advance to Initial Review
            </Button>
          </>
        ) : (
          <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-sm text-green-800 flex items-center gap-2">
            <CheckCircle size={14} />
            Soil test submitted.
          </div>
        )}
      </div>
    </Card>
  );
};
