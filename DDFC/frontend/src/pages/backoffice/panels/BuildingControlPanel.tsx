import React, { useState } from 'react';
import { Camera, AlertTriangle, CheckCircle, UploadCloud } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { submitBuildingControl, uploadSoilTest } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

const VIOLATION_CHECKS = [
  'Unauthorized construction',
  'Encroachment on public space',
  'Exceeds FAR limit',
  'Non-compliant setbacks',
  'Illegal additions / extensions',
  'Fire safety violations',
];

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const BuildingControlPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const [inspectionDate, setInspectionDate] = useState('');
  const [inspector, setInspector] = useState('');
  const [violations, setViolations] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  // Soil test fields
  const [soilTestDate, setSoilTestDate] = useState('');
  const [labName, setLabName] = useState('');
  const [soilBearingCapacity, setSoilBearingCapacity] = useState('');
  const [resultSummary, setResultSummary] = useState('');
  const [soilReportUrl, setSoilReportUrl] = useState('');
  const [soilSubmitting, setSoilSubmitting] = useState(false);

  const toggle = (v: string) =>
    setViolations((prev) =>
      prev.includes(v) ? prev.filter((x) => x !== v) : [...prev, v]
    );

  const handleSubmit = async () => {
    if (!inspectionDate || !inspector.trim() || violations.length > 0) return;
    setSubmitting(true);
    try {
      await dispatch(submitBuildingControl({
        id: requestId,
        data: { surveyDate: inspectionDate, officerName: inspector.trim(), violations: violations.length > 0 ? JSON.stringify(violations) : undefined },
      })).unwrap();
      toast.success('Building control clearance submitted');
    } catch {
      toast.error('Failed to submit clearance');
    } finally {
      setSubmitting(false);
    }
  };

  const handleSubmitSoilTest = async () => {
    if (!soilTestDate || !labName.trim() || !soilBearingCapacity || !resultSummary.trim() || !soilReportUrl.trim()) return;
    setSoilSubmitting(true);
    try {
      await dispatch(uploadSoilTest({
        id: requestId,
        data: { testDate: soilTestDate, labName: labName.trim(), soilBearingCapacity: parseFloat(soilBearingCapacity), resultSummary: resultSummary.trim(), reportFileUrl: soilReportUrl.trim() },
      })).unwrap();
      toast.success('Soil test report submitted');
      setSoilTestDate(''); setLabName(''); setSoilBearingCapacity(''); setResultSummary(''); setSoilReportUrl('');
    } catch {
      toast.error('Failed to submit soil test');
    } finally {
      setSoilSubmitting(false);
    }
  };

  return (
    <Card title="Building Control Panel">
      <div className="space-y-4">

        {/* Soil Test Report */}
        <div className="border border-red-200 rounded-lg p-4 bg-red-50">
          <h3 className="text-sm font-semibold text-red-700 mb-3 flex items-center gap-2">
            <UploadCloud size={14} />
            Soil Test Report <span className="text-red-500">*</span> (Required before clearance)
          </h3>
          {request.soilTestReport ? (
            <p className="text-xs text-green-700">
              ✓ Soil test report already uploaded.{' '}
              <a href={request.soilTestReport.reportUrl} target="_blank" rel="noopener noreferrer" className="underline">View</a>
            </p>
          ) : (
            <div className="space-y-3">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <Input label="Test Date" type="date" value={soilTestDate} onChange={(e) => setSoilTestDate(e.target.value)} />
                <Input label="Lab Name" placeholder="Name of testing lab" value={labName} onChange={(e) => setLabName(e.target.value)} />
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <Input label="Soil Bearing Capacity (kN/m²)" type="number" value={soilBearingCapacity} onChange={(e) => setSoilBearingCapacity(e.target.value)} />
                <Input label="Result Summary" placeholder="e.g. Suitable for construction" value={resultSummary} onChange={(e) => setResultSummary(e.target.value)} />
              </div>
              <div className="flex items-center gap-3">
                <FileUploadButton
                  label="Upload Soil Test Report (PDF)"
                  accept=".pdf"
                  onUploaded={(url) => setSoilReportUrl(url)}
                />
                {soilReportUrl && <span className="text-xs text-green-600">✓ Uploaded</span>}
              </div>
              <Button
                variant="secondary"
                size="sm"
                disabled={!soilTestDate || !labName.trim() || !soilBearingCapacity || !resultSummary.trim() || !soilReportUrl}
                loading={soilSubmitting}
                onClick={handleSubmitSoilTest}
                icon={<UploadCloud size={14} />}
              >
                Submit Soil Test Report
              </Button>
            </div>
          )}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="Inspection Date"
            type="date"
            value={inspectionDate}
            onChange={(e) => setInspectionDate(e.target.value)}
          />
          <Input
            label="Inspector Name"
            placeholder="Name of inspecting officer"
            value={inspector}
            onChange={(e) => setInspector(e.target.value)}
          />
        </div>

        {/* Violations Checklist */}
        <div>
          <h3 className="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-2">
            <AlertTriangle size={14} className="text-amber-500" />
            Violations Checklist
          </h3>
          <div className="space-y-2">
            {VIOLATION_CHECKS.map((v) => (
              <label key={v} className="flex items-center gap-2 text-sm cursor-pointer">
                <input
                  type="checkbox"
                  checked={violations.includes(v)}
                  onChange={() => toggle(v)}
                  className="rounded text-red-500"
                />
                <span className={violations.includes(v) ? 'text-red-700 font-medium' : 'text-gray-700'}>
                  {v}
                </span>
              </label>
            ))}
          </div>
          {violations.length > 0 && (
            <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded text-xs text-red-700">
              {violations.length} violation(s) flagged. Approval will be blocked.
            </div>
          )}
        </div>

        {/* Site Photos */}
        <div>
          <h3 className="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-2">
            <Camera size={14} />
            Inspection Photos
          </h3>
          <div className="border-2 border-dashed border-gray-200 rounded-lg p-4 text-center">
            <Camera className="mx-auto mb-1 text-gray-300" size={24} />
            <p className="text-xs text-gray-400 mb-2">JPG / PNG</p>
            <Button variant="secondary" size="sm" icon={<Camera size={14} />}>
              Upload Photos
            </Button>
          </div>
        </div>

        <Button
          variant="primary"
          className="w-full"
          disabled={!inspectionDate || !inspector || violations.length > 0}
          loading={submitting}
          onClick={handleSubmit}
          icon={<CheckCircle size={16} />}
        >
          {violations.length > 0 ? 'Cannot Approve — Violations Present' : 'Submit Building Control Clearance'}
        </Button>
      </div>
    </Card>
  );
};
