import type { RequestStatus } from '../../types';
import { Badge } from '../ui/Badge';

const statusConfig: Record<
  RequestStatus,
  { label: string; variant: 'info' | 'warning' | 'danger' | 'success' | 'secondary' | 'default' }
> = {
  Submitted: { label: 'Submitted', variant: 'info' },
  Initiated: { label: 'Initiated', variant: 'warning' },
  TransferApproved: { label: 'Transfer Approved', variant: 'info' },
  FinanceApproved: { label: 'Finance Approved', variant: 'info' },
  BothBranchesCleared: { label: 'Pending Possession', variant: 'warning' },
  PossessionIssued: { label: 'Possession Issued', variant: 'success' },
  PossessionLetterSigned: { label: 'Letter Signed', variant: 'success' },
  DelayUndertakingRequested: { label: 'Undertaking Requested', variant: 'warning' },
  DelayUndertakingSigned: { label: 'Undertaking Signed', variant: 'success' },
  PackageSelected: { label: 'Package Selected', variant: 'warning' },
  PackagePaid: { label: 'Package Paid', variant: 'success' },
  ArchitectAssigned: { label: 'Architect Assigned', variant: 'info' },
  ArchitectureApproved: { label: 'Architecture Approved', variant: 'info' },
  ThreeDCompleted: { label: '3D Completed', variant: 'info' },
  CadCompleted: { label: 'CAD Completed', variant: 'info' },
  StructureCompleted: { label: 'Structure Completed', variant: 'info' },
  MEPCompleted: { label: 'MEP Completed', variant: 'info' },
  PrincipalArchitectApproved: { label: 'Principal Arch. Approved', variant: 'secondary' },
  TownPlanningCompleted: { label: 'Building Control Done', variant: 'info' },
  BuildingControlCompleted: { label: 'Building Control Done', variant: 'info' },
  FinalApproved: { label: 'Final Approved', variant: 'success' },
  Delivered: { label: 'Delivered', variant: 'success' },
  Rejected: { label: 'Rejected', variant: 'danger' },
  OnHold: { label: 'On Hold', variant: 'default' },
};

interface StatusBadgeProps {
  status: RequestStatus;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status }) => {
  const cfg = statusConfig[status] ?? { label: status, variant: 'default' };
  return <Badge variant={cfg.variant}>{cfg.label}</Badge>;
};
