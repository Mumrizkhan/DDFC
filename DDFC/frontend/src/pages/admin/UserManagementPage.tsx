import React, { useEffect, useState } from 'react';
import { Plus, Search, Edit2, RotateCcw, UserX } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import {
  fetchUsers,
  createUser,
  updateUser,
  resetPassword,
} from '../../store/slices/adminSlice';
import { fetchDepartments, fetchRoles } from '../../store/slices/adminSlice';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Badge } from '../../components/ui/Badge';
import { Modal } from '../../components/ui/Modal';
import { Input, Select } from '../../components/ui/Input';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { format } from 'date-fns';
import { toast } from 'react-toastify';
import type { User } from '../../types';

const userSchema = z.object({
  fullName: z.string().min(1, 'Name is required'),
  email: z.string().email('Invalid email'),
  password: z.string().optional(),
  roleId: z.string().min(1, 'Role is required'),
  departmentId: z.string().optional(),
  isActive: z.boolean(),
});
type UserForm = z.infer<typeof userSchema>;

export const UserManagementPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { users, departments, roles, loading, actionLoading } = useAppSelector(
    (s) => s.admin
  );
  const [search, setSearch] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editUser, setEditUser] = useState<User | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<UserForm>({ resolver: zodResolver(userSchema) });

  useEffect(() => {
    dispatch(fetchUsers());
    dispatch(fetchDepartments());
    dispatch(fetchRoles());
  }, [dispatch]);

  const filtered = users.filter(
    (u) =>
      u.fullName.toLowerCase().includes(search.toLowerCase()) ||
      u.email.toLowerCase().includes(search.toLowerCase())
  );

  const openCreate = () => {
    setEditUser(null);
    reset({ isActive: true });
    setShowModal(true);
  };

  const openEdit = (user: User) => {
    setEditUser(user);
    reset({
      fullName: user.fullName,
      email: user.email,
      roleId: user.roleId,
      departmentId: user.departmentId ?? '',
      isActive: user.isActive,
    });
    setShowModal(true);
  };

  const onSubmit = async (data: UserForm) => {
    if (editUser) {
      await dispatch(updateUser({ userId: editUser.userId, data })).unwrap();
      toast.success('User updated');
    } else {
      await dispatch(createUser({ ...data, password: data.password ?? '' })).unwrap();
      toast.success('User created — temporary password sent via email');
    }
    setShowModal(false);
  };

  const handleResetPassword = async (userId: string) => {
    const newPassword = window.prompt('Enter new password for this user (min 8 chars):');
    if (!newPassword || newPassword.length < 8) {
      if (newPassword !== null) toast.error('Password must be at least 8 characters');
      return;
    }
    try {
      await dispatch(resetPassword({ userId, newPassword })).unwrap();
      toast.success('Password updated successfully');
    } catch {
      toast.error('Failed to reset password');
    }
  };

  const handleDeactivate = async (user: User) => {
    await dispatch(updateUser({ userId: user.userId, data: { isActive: false } })).unwrap();
    toast.success(`${user.fullName} deactivated`);
  };

  if (loading) return <PageLoader />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">User Management</h1>
          <p className="text-gray-500 text-sm mt-1">{users.length} users registered</p>
        </div>
        <Button onClick={openCreate}>
          <Plus size={16} /> Add User
        </Button>
      </div>

      <Card>
        {/* Search */}
        <div className="flex items-center gap-3 mb-4">
          <div className="relative flex-1">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Search by name or email..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100">
                <th className="text-left font-medium text-gray-500 pb-3">Name</th>
                <th className="text-left font-medium text-gray-500 pb-3">Email</th>
                <th className="text-left font-medium text-gray-500 pb-3">Role</th>
                <th className="text-left font-medium text-gray-500 pb-3">Department</th>
                <th className="text-left font-medium text-gray-500 pb-3">Status</th>
                <th className="text-left font-medium text-gray-500 pb-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {filtered.map((user) => (
                <tr key={user.userId} className="hover:bg-gray-50">
                  <td className="py-3 font-medium text-gray-900">{user.fullName}</td>
                  <td className="py-3 text-gray-600">{user.email}</td>
                  <td className="py-3">
                    <Badge variant="secondary">{user.roleName}</Badge>
                  </td>
                  <td className="py-3 text-gray-600">{user.departmentName ?? '—'}</td>
                  <td className="py-3">
                    <Badge variant={user.isActive ? 'success' : 'default'}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </td>
                  <td className="py-3">
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => openEdit(user)}
                        className="text-blue-600 hover:text-blue-800"
                        title="Edit"
                      >
                        <Edit2 size={14} />
                      </button>
                      <button
                        onClick={() => handleResetPassword(user.userId)}
                        className="text-yellow-600 hover:text-yellow-800"
                        title="Reset Password"
                      >
                        <RotateCcw size={14} />
                      </button>
                      {user.isActive && (
                        <button
                          onClick={() => handleDeactivate(user)}
                          className="text-red-600 hover:text-red-800"
                          title="Deactivate"
                        >
                          <UserX size={14} />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-gray-400">
                    No users found
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        title={editUser ? 'Edit User' : 'Create New User'}
        size="md"
        footer={
          <>
            <Button variant="outline" onClick={() => setShowModal(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleSubmit(onSubmit)}
              loading={actionLoading}
            >
              {editUser ? 'Save Changes' : 'Create User'}
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input
            label="Full Name"
            error={errors.fullName?.message}
            {...register('fullName')}
          />
          <Input
            label="Email"
            type="email"
            error={errors.email?.message}
            {...register('email')}
          />
          {!editUser && (
            <Input
              label="Password (leave blank to auto-generate)"
              type="password"
              {...register('password')}
            />
          )}
          <Select
            label="Role"
            error={errors.roleId?.message}
            options={[
              { value: '', label: 'Select role...' },
              ...roles.map((r) => ({ value: r.roleId, label: r.roleName })),
            ]}
            {...register('roleId')}
          />
          <Select
            label="Department"
            options={[
              { value: '', label: 'None' },
              ...departments.map((d) => ({
                value: d.departmentId,
                label: d.departmentName,
              })),
            ]}
            {...register('departmentId')}
          />
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" {...register('isActive')} className="rounded" />
            <span>Active</span>
          </label>
        </div>
      </Modal>
    </div>
  );
};
