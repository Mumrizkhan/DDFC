import React from 'react';
import { format } from 'date-fns';
import type { WorkflowHistoryEntry } from '../../types';
import { Badge } from '../ui/Badge';

interface WorkflowTimelineProps {
  history: WorkflowHistoryEntry[];
  showActor?: boolean;
}

const formatActionLabel = (entry: WorkflowHistoryEntry) => {
  const rawAction = entry.actionBy ?? '';

  const actionMap: Record<string, string> = {
    ApproveTransfer: 'Transfer Branch – NOC/NDC Review • Approve Transfer',
    RejectTransfer: 'Transfer Branch – NOC/NDC Review • Reject Transfer',
    TransferClarificationRequested: 'Transfer Branch – NOC/NDC Review • Request Clarification',
    ApproveFinance: 'Finance Branch – Dues Clearance • Approve Finance',
    RejectFinance: 'Finance Branch – Dues Clearance • Reject Finance',
    BcdUploadPossessionLetter: 'BCD – Upload Possession Letter • Upload Possession Letter',
    DdfcAdminSign: 'DDFC Admin – Sign Possession Letter • Sign Possession Letter',
    CompleteThreeD: '3D Visualization • Finalize 3D Visualization',
    Upload3DDraft: 'Architect • Upload 3D Draft',
    SelectPackage: 'Reception – Package Selection • Select Package',
    ConfirmPayment: 'Reception – Payment Confirmation • Confirm Payment',
    PrincipalApprove: 'Principal Architect • Approve Designs',
    FinalApprove: 'DHA Design Head • Final Approve',
    DeliverDocuments: 'Document Delivery • Deliver Documents',
    CreateRequest: 'Request Created',
    InitiateRequest: 'Request Submitted',
    VerifyDocuments: 'Documents Verified',
    DocumentsIncomplete: 'Documents Incomplete',
    TransferReject: 'Transfer Branch – NOC/NDC Review • Transfer Rejected',
    FinanceReject: 'Finance Branch – Dues Clearance • Finance Rejected',
    AdminReviewIncomplete: 'Admin Review • Marked Incomplete',
    AdminReviewReject: 'Admin Review • Rejected',
    AdminReviewInitiate: 'Admin Review • Documents Verification',
  };

  if (actionMap[rawAction]) return actionMap[rawAction];

  if (rawAction) {
    return rawAction
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/_/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
  }

  return `Workflow update: ${entry.toStatus}`;
};

export const WorkflowTimeline: React.FC<WorkflowTimelineProps> = ({
  history,
  showActor = true,
}) => (
  <div className="flow-root">
    <ul className="-mb-8">
      {history.map((entry, idx) => (
        <li key={entry.historyId}>
          <div className="relative pb-8">
            {idx < history.length - 1 && (
              <span
                className="absolute left-4 top-4 -ml-px h-full w-0.5 bg-gray-200"
                aria-hidden
              />
            )}
            <div className="relative flex space-x-3">
              <div>
                <span className="h-8 w-8 rounded-full bg-blue-500 flex items-center justify-center ring-8 ring-white">
                  <span className="text-white text-xs font-bold">
                    {formatActionLabel(entry).charAt(0).toUpperCase() || 'W'}
                  </span>
                </span>
              </div>
              <div className="flex min-w-0 flex-1 justify-between space-x-4 pt-1.5">
                <div>
                  <p className="text-sm text-gray-800">
                    {formatActionLabel(entry)}
                  </p>
                  {entry.toStatus && entry.toStatus !== 'BothBranchesCleared' && (
                    <div className="mt-1">
                      <Badge variant="info">
                        {entry.actionBy === 'VerifyDocuments' && entry.toStatus === 'DocumentsVerification'
                          ? 'Documents Verified'
                          : entry.toStatus === 'PrincipalArchitectReviewPending' ||
                            (entry.actionBy === 'CompleteMEP' && entry.toStatus === 'PrincipalArchitectApproved')
                            ? 'Principal Architect Review Pending'
                          : entry.toStatus}
                      </Badge>
                    </div>
                  )}
                  {entry.comments && (
                    <p className="mt-1 text-sm text-gray-500 italic">
                      "{entry.comments}"
                    </p>
                  )}
                  {showActor && entry.actorName && (
                    <p className="mt-0.5 text-xs text-gray-400">
                      by {entry.actorName}
                    </p>
                  )}
                </div>
                <div className="whitespace-nowrap text-right text-xs text-gray-400">
                  {format(new Date(entry.timestamp), 'dd MMM yyyy HH:mm')}
                </div>
              </div>
            </div>
          </div>
        </li>
      ))}
    </ul>
  </div>
);
