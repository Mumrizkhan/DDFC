import React, { useEffect, useState } from 'react';
import { Users, UploadCloud, FileCheck, CheckCircle, Layers } from 'lucide-react';
import { Button } from '../ui/Button';
import { Select } from '../ui/Input';
import { FileUploadButton } from '../ui/FileUploadButton';
import { toast } from 'react-toastify';
import { useAppDispatch } from '../../store/hooks';
import { assignCadOperator, submitDeptCadFile } from '../../store/slices/requestsSlice';
import type { CadType, PossessionRequest } from '../../types';
import api from '../../services/api';

interface StaffUser {
  id: string;
  fullName: string;
}

interface Props {
  cadType: CadType;
  label: string;
  requestId: string;
  request: PossessionRequest;
  /** Tailwind border+bg class for the section card, e.g. 'border-blue-200 bg-blue-50' */
  accentClass?: string;
}

export const CadSection: React.FC<Props> = ({
  cadType,
  label,
  requestId,
  request,
  accentClass = 'border-orange-200 bg-orange-50',
}) => {
  const dispatch = useAppDispatch();

  const [users, setUsers]                 = useState<StaffUser[]>([]);
  const [selectedUserId, setSelectedUser] = useState('');
  const [assigning, setAssigning]         = useState(false);
  const [fileUrl, setFileUrl]             = useState('');
  const [submitting, setSubmitting]       = useState(false);

  const existing = request.cadAssignments?.find((a) => a.cadType === cadType);

  useEffect(() => {
    api
      .get<StaffUser[] | { users: StaffUser[] }>('/admin/users')
      .then((r) => {
        const raw = r.data;
        setUsers(Array.isArray(raw) ? raw : (raw as { users: StaffUser[] }).users ?? []);
      })
      .catch(() => {});
  }, []);

  const handleAssign = async () => {
    if (!selectedUserId) { toast.error('Please select a CAD operator'); return; }
    setAssigning(true);
    try {
      await dispatch(
        assignCadOperator({ id: requestId, data: { cadType, assignedUserId: selectedUserId } })
      ).unwrap();
      toast.success(`${label} assigned`);
      setSelectedUser('');
    } catch {
      toast.error('Failed to assign CAD operator');
    } finally {
      setAssigning(false);
    }
  };

  const handleSubmitFile = async () => {
    if (!fileUrl) { toast.error('Please upload a file first'); return; }
    const ext = fileUrl.split('.').pop()?.toLowerCase() ?? 'file';
    setSubmitting(true);
    try {
      await dispatch(
        submitDeptCadFile({
          id: requestId,
          data: { cadType, fileUrl, fileType: ext },
        })
      ).unwrap();
      toast.success(`${label} file submitted`);
      setFileUrl('');
    } catch {
      toast.error(`Failed to submit ${label} file`);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className={`border rounded-lg p-4 space-y-3 ${accentClass}`}>

      {/* Header */}
      <div className="flex items-center gap-2">
        <Layers size={14} className="text-gray-500 shrink-0" />
        <h4 className="text-sm font-semibold text-gray-700">{label}</h4>
        {existing?.completedAt && (
          <span className="ml-auto flex items-center gap-1 text-xs text-green-700 font-medium">
            <CheckCircle size={12} /> Done
          </span>
        )}
      </div>

      {/* Current assignment badge */}
      {existing?.assignedUserName ? (
        <div className="flex items-center gap-1.5 text-xs text-gray-600">
          <Users size={12} className="shrink-0" />
          Assigned to:{' '}
          <span className="font-medium text-gray-800">{existing.assignedUserName}</span>
        </div>
      ) : (
        <p className="text-xs text-gray-400 italic">No CAD operator assigned yet</p>
      )}

      {/* ── Assign operator ── */}
      <div className="flex gap-2 items-end">
        <div className="flex-1">
          <Select
            label="Assign CAD Operator"
            value={selectedUserId}
            onChange={(e) => setSelectedUser(e.target.value)}
            options={[
              { value: '', label: users.length ? '— Select operator —' : 'Loading…' },
              ...users.map((u) => ({ value: u.id, label: u.fullName })),
            ]}
          />
        </div>
        <Button
          variant="outline"
          size="sm"
          loading={assigning}
          disabled={!selectedUserId}
          onClick={handleAssign}
          icon={<Users size={13} />}
          className="shrink-0 mb-0.5"
        >
          {existing?.assignedUserName ? 'Reassign' : 'Assign'}
        </Button>
      </div>

      {/* ── CAD file upload ── */}
      <div className="space-y-2 pt-1 border-t border-black/5">
        <p className="text-xs font-medium text-gray-500">{label} File</p>

        {/* Existing submitted file */}
        {existing?.fileUrl && (
          <div className="flex items-center gap-2 text-xs bg-green-50 border border-green-200 rounded p-2">
            <FileCheck size={12} className="text-green-600 shrink-0" />
            <a
              href={existing.fileUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-green-700 underline truncate"
            >
              {existing.fileName ?? 'View submitted file'}
            </a>
            {existing.fileType && (
              <span className="text-gray-400 uppercase shrink-0">.{existing.fileType}</span>
            )}
          </div>
        )}

        <FileUploadButton
          label={`Upload ${label} (.dwg or .pdf)`}
          accept=".dwg,.pdf"
          value={fileUrl}
          onUploaded={setFileUrl}
        />

        {fileUrl && (
          <Button
            variant="primary"
            size="sm"
            loading={submitting}
            onClick={handleSubmitFile}
            icon={<UploadCloud size={13} />}
          >
            Submit {label} File
          </Button>
        )}
      </div>
    </div>
  );
};
