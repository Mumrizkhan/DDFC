import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchRequests } from '../../store/slices/requestsSlice';
import { Card } from '../../components/ui/Card';
import { StatusBadge } from '../../components/shared/StatusBadge';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { Select } from '../../components/ui/Input';
import { format } from 'date-fns';

const STATUS_OPTIONS: { value: string; label: string }[] = [
  { value: '', label: 'All Statuses' },
  { value: 'Submitted', label: 'Submitted' },
  { value: 'DocumentsVerification', label: 'Documents Verification' },
  { value: 'TransferApproved', label: 'Transfer Approved' },
  { value: 'FinanceApproved', label: 'Finance Approved' },
  { value: 'BothBranchesCleared', label: 'Pending Possession' },
  { value: 'AdminReviewPending', label: 'Admin Review Pending' },
  { value: 'AdCoordApproved', label: 'AD Coordinator Approved' },
  { value: 'BcdLetterUploaded', label: 'BCD Letter Uploaded' },
  { value: 'PossessionIssued', label: 'Possession Issued' },
  { value: 'PackageSelected', label: 'Package Selected' },
  { value: 'PackagePaid', label: 'Package Paid' },
  { value: 'SoilTestCompleted', label: 'Soil Test Completed' },
  { value: 'ArchitectureApproved', label: 'Architecture Approved' },
  { value: 'PrincipalArchitectReviewPending', label: 'Principal Architect Review Pending' },
  { value: 'FinalApproved', label: 'Final Approved' },
  { value: 'Delivered', label: 'Delivered' },
  { value: 'Rejected', label: 'Rejected' },
];

export const RequestsListPage: React.FC = () => {
  // ── Hooks (must be called at top level before any early returns) ─────────────
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { requests, loading, totalCount, error } = useAppSelector((s) => s.requests);
  const staffUser = useAppSelector((s) => s.auth.staffUser);
  const [search, setSearch] = useState('');

  const isTransferOfficer   = staffUser?.roleName === 'Transfer Officer';
  const isFinanceOfficer    = staffUser?.roleName === 'Finance Officer';
  const isAdCoordinator     = staffUser?.roleName === 'AD Coordinator';
  const isBuildingControlOfficer = staffUser?.roleName === 'Building Control Officer';
  const isThreeDUser = staffUser?.roleName === '3D Engineer' || staffUser?.roleName === '3D Operator';
  const isRestrictedRole    = isTransferOfficer || isFinanceOfficer || isAdCoordinator || isBuildingControlOfficer || isThreeDUser;

  // Statuses each restricted role needs to action
  const roleStatusFilter: string[] | undefined = isTransferOfficer
    ? ['DocumentsVerification', 'FinanceApproved']
    : isFinanceOfficer
    ? ['DocumentsVerification', 'TransferApproved']
    : isAdCoordinator
    ? ['BothBranchesCleared']
    : isBuildingControlOfficer
    ? ['AdCoordApproved', 'PackagePaid', 'PrincipalArchitectApproved']
    : isThreeDUser
    ? ['ArchitectureApproved']
    : undefined;

  const [statusFilter, setStatusFilter] = useState('');

  useEffect(() => {
    dispatch(fetchRequests({ status: roleStatusFilter ?? (statusFilter || undefined) }));
  }, [dispatch, statusFilter]); // eslint-disable-line react-hooks/exhaustive-deps

  // ── Early returns ─────────────────────────────────────────────────────────────
  if (loading) return <PageLoader />;

  if (error) {
    return (
      <div className="max-w-2xl mx-auto p-4">
        <Card>
          <div className="p-6 text-center">
            <p className="text-red-600 font-medium mb-4">Error loading requests</p>
            <p className="text-gray-600 text-sm mb-6">{error}</p>
            <button
              onClick={() => dispatch(fetchRequests({ status: roleStatusFilter ?? (statusFilter || undefined) }))}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              Try Again
            </button>
          </div>
        </Card>
      </div>
    );
  }

  // ── Data filtering ────────────────────────────────────────────────────────────
  const filtered = requests.filter((r) => {
    if (!search) return true;
    const s = search.toLowerCase();
    return (
      r.requestId.toLowerCase().includes(s) ||
      (r.customerName ?? '').toLowerCase().includes(s) ||
      (r.plotNumber ?? '').toLowerCase().includes(s) ||
      r.fileNo.toLowerCase().includes(s)
    );
  });

  // ── Permissions check ─────────────────────────────────────────────────────────
  let canCreateRequest = false;
  
  // Log for debugging
  console.log('Staff User:', staffUser);
  console.log('Raw permissions:', staffUser?.permissions);
  
  try {
    if (staffUser?.permissions) {
      let perms: Record<string, boolean> | string[] | string = staffUser.permissions;
      
      // Handle string (JSON array or JSON object)
      if (typeof perms === 'string') {
        console.log('Parsing permissions from string:', perms);
        perms = JSON.parse(perms);
      }
      
      // Check if it's an array of permission strings
      if (Array.isArray(perms)) {
        console.log('Permissions are array:', perms);
        canCreateRequest = perms.includes('CanCreateRequest');
      } 
      // Check if it's an object with permission keys
      else if (typeof perms === 'object' && perms !== null) {
        console.log('Permissions are object:', perms);
        canCreateRequest = !!(perms as Record<string, boolean>)['CanCreateRequest'];
      }
      
      console.log('Can create request:', canCreateRequest);
    } else {
      console.log('No permissions found on staff user');
    }
  } catch (err) {
    console.error('Failed to parse permissions:', err);
  }

  const canInitiateRequest = false;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            {isRestrictedRole ? 'My Work Queue' : 'All Requests'}
          </h1>
          <p className="text-gray-500 text-sm mt-1">
            {isRestrictedRole
              ? `Showing requests pending your action`
              : `${totalCount} total requests in system`}
          </p>
        </div>
        {canCreateRequest && (
          <button
            className="inline-flex items-center gap-2 px-4 py-2 bg-emerald-600 text-white rounded-lg shadow hover:bg-emerald-700 transition-colors"
            onClick={() => navigate('/backoffice/requests/create')}
          >
            <span>New Request</span>
            <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="lucide lucide-plus"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
          </button>
        )}
      </div>

      <Card>
        {/* Filters */}
        <div className="flex gap-3 mb-4">
          <div className="relative flex-1">
            <Search
              size={16}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
            />
            <input
              type="text"
              placeholder="Search by request ID, customer, plot, file no..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          {!isRestrictedRole && (
            <Select
              options={STATUS_OPTIONS}
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="w-48"
            />
          )}
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100">
                <th className="text-left font-medium text-gray-500 pb-3">Request ID</th>
                <th className="text-left font-medium text-gray-500 pb-3">Customer</th>
                <th className="text-left font-medium text-gray-500 pb-3">Plot</th>
                <th className="text-left font-medium text-gray-500 pb-3">File No</th>
                <th className="text-left font-medium text-gray-500 pb-3">Status</th>
                <th className="text-left font-medium text-gray-500 pb-3">Submitted</th>
                <th className="text-left font-medium text-gray-500 pb-3">Updated</th>
                {canInitiateRequest && <th className="text-left font-medium text-gray-500 pb-3">Action</th>}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {filtered.map((req) => (
                <tr
                  key={req.requestId}
                  className="hover:bg-blue-50 cursor-pointer transition-colors"
                  onClick={() => navigate(`/backoffice/requests/${req.requestId}`)}
                >
                  <td className="py-3 font-mono text-blue-600 font-medium">
                    {req.requestId}
                  </td>
                  <td className="py-3 text-gray-900">{req.customerName ?? '—'}</td>
                  <td className="py-3 text-gray-600">
                    {req.plotNumber
                      ? `${req.plotNumber} / ${req.sectorNo}`
                      : '—'}
                  </td>
                  <td className="py-3 text-gray-600">{req.fileNo}</td>
                  <td className="py-3">
                    <StatusBadge status={req.status} activeStepNames={req.activeWorkflowStepNames} />
                  </td>
                  <td className="py-3 text-gray-500">
                    {format(new Date(req.submittedAt), 'dd MMM yyyy')}
                  </td>
                  <td className="py-3 text-gray-500">
                    {req.updatedAt ? format(new Date(req.updatedAt), 'dd MMM yyyy') : '—'}
                  </td>
                  {canInitiateRequest && (
                    <td className="py-3" onClick={(e) => e.stopPropagation()}>
                      {req.status === 'Submitted' && (
                        <button
                          onClick={() => navigate(`/backoffice/requests/${req.requestId}?tab=Actions`)}
                          className="text-xs font-medium text-emerald-700 bg-emerald-50 border border-emerald-200 px-2 py-1 rounded hover:bg-emerald-100 transition-colors whitespace-nowrap"
                        >
                          Initiate →
                        </button>
                      )}
                    </td>
                  )}
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={canInitiateRequest ? 8 : 7} className="py-10 text-center text-gray-400">
                    No requests found
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
};
