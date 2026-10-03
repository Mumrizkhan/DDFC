import type { RequestStatus } from '../../types';
import { Badge } from '../ui/Badge';

const statusConfig: Record<
  RequestStatus,
  { label: string; variant: 'info' | 'warning' | 'danger' | 'success' | 'secondary' | 'default' }
> = {
  Submitted: { label: 'Submitted', variant: 'info' },
  DocumentsVerification: { label: 'Documents Verification', variant: 'success' },
  TransferApproved: { label: 'Transfer Approved', variant: 'info' },
  FinanceApproved: { label: 'Finance Approved', variant: 'info' },
  BothBranchesCleared: { label: 'Pending Possession', variant: 'warning' },
  AdminReviewPending: { label: 'Admin Review Pending', variant: 'warning' },
  AdCoordApproved: { label: 'AD Coordinator Approved', variant: 'info' },
  BcdLetterUploaded: { label: 'BCD Letter Uploaded', variant: 'info' },
  PossessionIssued: { label: 'Possession Issued', variant: 'success' },
  PossessionLetterSigned: { label: 'Letter Signed', variant: 'success' },
  DelayUndertakingRequested: { label: 'Undertaking Requested', variant: 'warning' },
  DelayUndertakingSigned: { label: 'Undertaking Signed', variant: 'success' },
  PackageSelected: { label: 'Package Selected', variant: 'warning' },
  PackagePaid: { label: 'Package Paid', variant: 'success' },
  SoilTestCompleted: { label: 'Soil Test Completed', variant: 'success' },
  ArchitectAssigned: { label: 'Architect Assigned', variant: 'info' },
  ArchitectureApproved: { label: 'Architecture Approved', variant: 'info' },
  ThreeDCompleted: { label: '3D Completed', variant: 'info' },
  ThreeDDraftPending: { label: '3D Draft Pending', variant: 'warning' },
  ThreeDDraftUploaded: { label: '3D Draft Uploaded', variant: 'info' },
  CadCompleted: { label: 'CAD Completed', variant: 'info' },
  StructureCompleted: { label: 'Structure Completed', variant: 'info' },
  MEPCompleted: { label: 'MEP Completed', variant: 'info' },
  PrincipalArchitectReviewPending: { label: 'Principal Architect Review Pending', variant: 'warning' },
  PrincipalArchitectApproved: { label: 'Principal Architect Approved', variant: 'secondary' },
  TownPlanningCompleted: { label: 'Building Control Done', variant: 'info' },
  BuildingControlCompleted: { label: 'Building Control Done', variant: 'info' },
  FinalApproved: { label: 'Final Approved', variant: 'success' },
  Delivered: { label: 'Delivered', variant: 'success' },
  Rejected: { label: 'Rejected', variant: 'danger' },
  OnHold: { label: 'On Hold', variant: 'default' },
};

interface StatusBadgeProps {
  status: RequestStatus;
  activeStepNames?: string[];
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status, activeStepNames }) => {
  if (status === 'PrincipalArchitectApproved' && activeStepNames?.some((name) =>
      name.toLowerCase().includes('principal architect') && name.toLowerCase().includes('design review'))) {
    return <Badge variant="warning">Principal Architect Review Pending</Badge>;
  }
  if ((status === 'DocumentsVerification' || status === 'PackagePaid' || status === 'ArchitectureApproved') && activeStepNames?.length &&
      activeStepNames.every((name) => !/documents? verification|document review/i.test(name))) {
    return <Badge variant="warning">{activeStepNames.join(' / ')}</Badge>;
  }
  const cfg = statusConfig[status] ?? { label: status, variant: 'default' };
  return <Badge variant={cfg.variant}>{cfg.label}</Badge>;
};
