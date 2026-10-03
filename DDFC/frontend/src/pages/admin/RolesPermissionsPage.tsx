import React, { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import {
  fetchRoles,
  updateRolePermissions,
} from '../../store/slices/adminSlice';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { Badge } from '../../components/ui/Badge';
import { toast } from 'react-toastify';
import type { Role } from '../../types';

const ALL_PERMISSIONS = [
  'CanCreateRequest',
  'CanApproveTransfer',
  'CanApproveFinance',
  'CanApproveAdCoord',
  'CanIssuePossession',
  'CanSelectPackage',
  'CanConfirmPayment',
  'CanUploadPlan',
  'CanApprovePlan',
  'CanComplete3D',
  'CanAssign3DOperator',
  'CanCompleteStructure',
  'CanCompleteMEP',
  'CanApprovePrincipal',
  'CanSubmitBuildingControl',
  'CanFinalApprove',
  'CanViewAllRequests',
  'CanAssignTasks',
  'CanReassignTasks',
  'CanConfigurePackages',
  'CanManageUsers',
  'CanManageDepartments',
  'CanManageTemplates',
  'CanViewReports',
  'CanManageSettings',
  'CanManageRoles',
];

const PROTECTED_ROLES = ['Admin', 'Manager', 'Employee'];

const normalizePermissions = (value?: Role['permissions']): Record<string, boolean> => {
  if (!value) return {};

  if (typeof value === 'string') {
    try {
      const parsed = JSON.parse(value);
      if (Array.isArray(parsed)) {
        return Object.fromEntries(parsed.map((permission) => [permission, true]));
      }
      if (parsed && typeof parsed === 'object') {
        return parsed as Record<string, boolean>;
      }
      return {};
    } catch {
      return {};
    }
  }

  if (Array.isArray(value)) {
    return Object.fromEntries(value.map((permission) => [permission, true]));
  }

  return value;
};

export const RolesPermissionsPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { roles, loading, actionLoading } = useAppSelector((s) => s.admin);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [permissions, setPermissions] = useState<Record<string, boolean>>({});

  useEffect(() => {
    dispatch(fetchRoles());
  }, [dispatch]);

  const activeRole = selectedRole ?? roles[0] ?? null;
  const activePermissions = activeRole && !selectedRole
    ? normalizePermissions(activeRole.permissions)
    : permissions;

  const handleSelectRole = (role: Role) => {
    setSelectedRole(role);
    setPermissions(normalizePermissions(role.permissions));
  };

  const togglePermission = (perm: string) => {
    if (PROTECTED_ROLES.includes(activeRole?.roleName ?? '')) return;
    setPermissions((prev) => ({
      ...prev,
      [perm]: !prev[perm],
    }));
  };

  const handleSave = async () => {
    const targetRole = activeRole;
    if (!targetRole) return;
    const enabledPerms = Object.entries(activePermissions)
      .filter(([, value]) => value)
      .map(([key]) => key);
    await dispatch(
      updateRolePermissions({ roleId: targetRole.roleId, permissions: JSON.stringify(enabledPerms) })
    ).unwrap();
    toast.success(`Permissions updated for ${targetRole.roleName}`);
  };

  if (loading) return <PageLoader />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Roles & Permissions</h1>
        <p className="text-gray-500 text-sm mt-1">
          Manage role-based access control. Changes take effect on next user login.
        </p>
      </div>

      <div className="flex gap-6">
        {/* Roles list */}
        <div className="w-64 flex-shrink-0">
          <Card title="Roles">
            <div className="space-y-1">
              {roles.map((role) => (
                <button
                  key={role.roleId}
                  onClick={() => handleSelectRole(role)}
                  className={`w-full text-left px-3 py-2.5 rounded-lg text-sm transition-colors
                    ${selectedRole?.roleId === role.roleId
                      ? 'bg-blue-600 text-white'
                      : 'hover:bg-gray-100 text-gray-700'}`}
                >
                  <div className="flex items-center justify-between">
                    <span>{role.roleName}</span>
                    {PROTECTED_ROLES.includes(role.roleName) && (
                      <Badge variant="secondary" className="text-xs">
                        protected
                      </Badge>
                    )}
                  </div>
                </button>
              ))}
            </div>
          </Card>
        </div>

        {/* Permissions editor */}
        <div className="flex-1">
          {selectedRole ? (
            <Card
              title={`Permissions — ${selectedRole.roleName}`}
              action={
                !PROTECTED_ROLES.includes(selectedRole.roleName) ? (
                  <Button size="sm" onClick={handleSave} loading={actionLoading}>
                    Save Changes
                  </Button>
                ) : (
                  <Badge variant="default">Read-only</Badge>
                )
              }
            >
              <div className="grid grid-cols-2 gap-3">
                {ALL_PERMISSIONS.map((perm) => (
                  <label
                    key={perm}
                    className={`flex items-center gap-3 p-3 rounded-lg border transition-colors cursor-pointer
                      ${activePermissions[perm] ? 'border-blue-300 bg-blue-50' : 'border-gray-200 bg-white'}
                      ${PROTECTED_ROLES.includes(activeRole?.roleName ?? '') ? 'opacity-60 cursor-not-allowed' : ''}`}
                  >
                    <input
                      type="checkbox"
                      checked={!!activePermissions[perm]}
                      onChange={() => togglePermission(perm)}
                      disabled={PROTECTED_ROLES.includes(activeRole?.roleName ?? '')}
                      className="rounded text-blue-600"
                    />
                    <span className="text-sm text-gray-700">{perm}</span>
                  </label>
                ))}
              </div>
            </Card>
          ) : (
            <p className="text-gray-400 text-sm">Select a role to edit permissions.</p>
          )}
        </div>
      </div>
    </div>
  );
};
