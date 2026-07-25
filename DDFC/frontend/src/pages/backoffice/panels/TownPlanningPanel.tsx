import React, { useState } from 'react';
import { UploadCloud, Camera } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { submitTownPlanning, uploadSoilTest } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const TownPlanningPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const [surveyDate, setSurveyDate] = useState('');
  const [officerName, setOfficerName] = useState('');
  const [observations, setObservations] = useState('');
  const [violations, setViolations] = useState('');
  const [submitting, setSubmitting] = useState(false);

  // Soil test fields
  const [soilTestDate, setSoilTestDate] = useState('');
  const [labName, setLabName] = useState('');
  const [soilBearingCapacity, setSoilBearingCapacity] = useState('');
  const [resultSummary, setResultSummary] = useState('');
  const [soilReportUrl, setSoilReportUrl] = useState('');
  const [soilSubmitting, setSoilSubmitting] = useState(false);

  const handleSubmitSurvey = async () => {
    if (!surveyDate || !officerName.trim()) return;
    setSubmitting(true);
    try {
      await dispatch(submitTownPlanning({
        id: requestId,
        data: { surveyDate, officerName: officerName.trim(), observations: observations.trim() || undefined, violations: violations.trim() || undefined },
      })).unwrap();
      toast.success('Town planning survey submitted');
    } catch {
      toast.error('Failed to submit survey');
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
    <Card title="Town Planning Panel">
      <div className="space-y-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="Survey Date"
            type="date"
            value={surveyDate}
            onChange={(e) => setSurveyDate(e.target.value)}
          />
          <Input
            label="Officer Name"
            placeholder="Name of survey officer"
            value={officerName}
            onChange={(e) => setOfficerName(e.target.value)}
          />
        </div>

        <div>
          <Input
            label="Observations (optional)"
            placeholder="Any observations from the survey..."
            value={observations}
            onChange={(e) => setObservations(e.target.value)}
          />
        </div>

        <div>
          <Input
            label="Violations (optional)"
            placeholder="Any zoning or land-use violations..."
            value={violations}
            onChange={(e) => setViolations(e.target.value)}
          />
        </div>

        {/* Soil Test Report */}
        <div className="border border-red-200 rounded-lg p-4 bg-red-50">
          <h3 className="text-sm font-semibold text-red-700 mb-3 flex items-center gap-2">
            <UploadCloud size={14} />
            Soil Test Report <span className="text-red-500">*</span> (Required)
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
              </div>
              <Input label="Result Summary" placeholder="Brief summary of soil test results" value={resultSummary} onChange={(e) => setResultSummary(e.target.value)} />
              <FileUploadButton
                label="Soil Test Report File"
                required
                value={soilReportUrl}
                onUploaded={(url) => setSoilReportUrl(url)}
              />
              <Button
                variant="danger"
                size="sm"
                loading={soilSubmitting}
                disabled={!soilTestDate || !labName.trim() || !soilBearingCapacity || !resultSummary.trim() || !soilReportUrl}
                onClick={handleSubmitSoilTest}
                icon={<UploadCloud size={14} />}
              >
                Submit Soil Test
              </Button>
            </div>
          )}
        </div>

        {/* Site Photos note */}
        <div>
          <h3 className="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-2">
            <Camera size={14} />
            Site Photos (optional)
          </h3>
          <p className="text-xs text-gray-400">Add photo URLs to the observations field above, separated by commas.</p>
        </div>

        <Button
          variant="primary"
          className="w-full"
          disabled={!surveyDate || !officerName.trim()}
          loading={submitting}
          onClick={handleSubmitSurvey}
        >
          Submit Town Planning Survey
        </Button>
      </div>
    </Card>
  );
};
