import React from 'react';
import { FileText, Box, Layers, Zap, CheckCircle, Clock } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { format } from 'date-fns';
import type { PossessionRequest } from '../../../types';

interface Props {
  request: PossessionRequest;
}

export const PrincipalArchitectDesignReviewPanel: React.FC<Props> = ({ request }) => {
  const docs = request.documents ?? [];

  const planDocs = docs.filter((d) => d.documentType.toLowerCase().includes('plan') || d.documentType.toLowerCase().includes('architectural'));
  const structureDocs = docs.filter((d) => d.documentType.toLowerCase().includes('structural'));
  const mepDocs = docs.filter((d) => d.documentType.toLowerCase().includes('mep'));
  const threeDFiles = request.threeDVisualizations ?? [];

  const Section: React.FC<{
    icon: React.ReactNode;
    title: string;
    done: boolean;
    children: React.ReactNode;
  }> = ({ icon, title, done, children }) => (
    <div className="border border-gray-200 rounded-lg overflow-hidden">
      <div className={`flex items-center gap-2 px-4 py-2.5 text-sm font-semibold ${done ? 'bg-green-50 text-green-800' : 'bg-gray-50 text-gray-500'}`}>
        {icon}
        <span>{title}</span>
        <span className={`ml-auto flex items-center gap-1 text-xs font-normal ${done ? 'text-green-600' : 'text-amber-500'}`}>
          {done ? <CheckCircle size={12} /> : <Clock size={12} />}
          {done ? 'Complete' : 'Pending'}
        </span>
      </div>
      <div className="p-3 space-y-1.5">{children}</div>
    </div>
  );

  const DocLink: React.FC<{ label: string; url: string; uploadedAt: string }> = ({ label, url, uploadedAt }) => (
    <div className="flex items-center justify-between text-sm">
      <span className="text-gray-700 truncate mr-2">{label}</span>
      <div className="shrink-0 flex items-center gap-2 text-xs text-gray-400">
        <span>{format(new Date(uploadedAt), 'dd MMM yyyy')}</span>
        <a href={url} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline">
          View
        </a>
      </div>
    </div>
  );

  return (
    <Card title="Principal Architect – Design Review">
      <div className="space-y-4">
        <p className="text-sm text-gray-500">
          All parallel design branches have been completed. Review each submission below, then use
          the <strong>Actions</strong> tab to approve or send back for revision.
        </p>

        {/* House Plan */}
        <Section icon={<FileText size={14} />} title="Architectural House Plan" done={planDocs.length > 0}>
          {planDocs.length > 0
            ? planDocs.map((d) => (
                <DocLink key={d.documentId} label={d.documentType} url={d.fileUrl} uploadedAt={d.uploadedAt} />
              ))
            : <p className="text-xs text-gray-400">No plan uploaded yet.</p>}
        </Section>

        {/* 3D Visualization */}
        <Section icon={<Box size={14} />} title="3D Visualization" done={threeDFiles.length > 0}>
          {threeDFiles.length > 0
            ? threeDFiles.map((f) => (
                <DocLink key={f.id} label={f.fileName} url={f.fileUrl} uploadedAt={f.uploadedAt} />
              ))
            : <p className="text-xs text-gray-400">No 3D files uploaded yet.</p>}
        </Section>

        {/* Structural Design */}
        <Section icon={<Layers size={14} />} title="Structural Design" done={!!request.structureCompleted}>
          {structureDocs.length > 0
            ? structureDocs.map((d) => (
                <DocLink key={d.documentId} label={d.documentType} url={d.fileUrl} uploadedAt={d.uploadedAt} />
              ))
            : <p className="text-xs text-gray-400">No structural report uploaded yet.</p>}
        </Section>

        {/* MEP Design */}
        <Section icon={<Zap size={14} />} title="MEP Design" done={!!request.mepCompleted}>
          {mepDocs.length > 0
            ? mepDocs.map((d) => (
                <DocLink key={d.documentId} label={d.documentType} url={d.fileUrl} uploadedAt={d.uploadedAt} />
              ))
            : <p className="text-xs text-gray-400">No MEP report uploaded yet.</p>}
        </Section>
      </div>
    </Card>
  );
};
