import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAppSelector } from './store/hooks';

// Layouts
import { AdminLayout } from './layouts/AdminLayout';
import { BackOfficeLayout } from './layouts/BackOfficeLayout';
import { CustomerLayout } from './layouts/CustomerLayout';

// Auth
import { StaffLoginPage } from './pages/auth/StaffLoginPage';
import { CustomerLoginPage } from './pages/auth/CustomerLoginPage';

// Admin
import { AdminDashboardPage } from './pages/admin/AdminDashboardPage';
import { UserManagementPage } from './pages/admin/UserManagementPage';
import { DepartmentManagementPage } from './pages/admin/DepartmentManagementPage';
import { RolesPermissionsPage } from './pages/admin/RolesPermissionsPage';
import { PackagePricingPage } from './pages/admin/PackagePricingPage';
import { SystemSettingsPage } from './pages/admin/SystemSettingsPage';
import { ReportsPage } from './pages/admin/ReportsPage';
import { TemplateManagementPage } from './pages/admin/TemplateManagementPage';

// Back Office
import { BackOfficeDashboardPage } from './pages/backoffice/BackOfficeDashboardPage';
import { RequestsListPage } from './pages/backoffice/RequestsListPage';
import { CreateRequestPage } from './pages/backoffice/CreateRequestPage';
import { TaskDetailPage } from './pages/backoffice/TaskDetailPage';
import { TasksPage } from './pages/backoffice/TasksPage';
import { AppointmentsPage } from './pages/backoffice/AppointmentsPage';

// Technical Support
import { TechSupportDashboardPage } from './pages/technical-support/TechSupportDashboardPage';
import { TicketsQueuePage } from './pages/technical-support/TicketsQueuePage';
import { TicketSupportDetailPage } from './pages/technical-support/TicketSupportDetailPage';

// Customer
import { CustomerDashboardPage } from './pages/customer/CustomerDashboardPage';
import { RequestDetailPage } from './pages/customer/RequestDetailPage';
import { NewRequestPage } from './pages/customer/NewRequestPage';
import { PaymentsPage } from './pages/customer/PaymentsPage';
import { TicketsPage } from './pages/customer/TicketsPage';
import { TicketDetailPage } from './pages/customer/TicketDetailPage';
import { NotificationsPage } from './pages/customer/NotificationsPage';
import { ProfilePage } from './pages/customer/ProfilePage';

// Guards
import { ProtectedRoute } from './components/ProtectedRoute';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Auth */}
        <Route path="/staff/login" element={<StaffLoginPage />} />
        <Route path="/portal/login" element={<CustomerLoginPage />} />

        {/* Admin Portal */}
        <Route
          path="/admin"
          element={
            <ProtectedRoute requireRole="Admin">
              <AdminLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Navigate to="dashboard" replace />} />
          <Route path="dashboard" element={<AdminDashboardPage />} />
          <Route path="users" element={<UserManagementPage />} />
          <Route path="departments" element={<DepartmentManagementPage />} />
          <Route path="roles" element={<RolesPermissionsPage />} />
          <Route path="packages" element={<PackagePricingPage />} />
          <Route path="settings" element={<SystemSettingsPage />} />
          <Route path="reports" element={<ReportsPage />} />
          <Route path="templates" element={<TemplateManagementPage />} />
        </Route>

        {/* Back Office */}
        <Route
          path="/backoffice"
          element={
            <ProtectedRoute requireRole="Staff">
              <BackOfficeLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Navigate to="dashboard" replace />} />
          <Route path="dashboard" element={<BackOfficeDashboardRouter />} />
          <Route path="requests" element={<RequestsListPage />} />
          <Route path="requests/create" element={<CreateRequestPage />} />
          <Route path="requests/:requestId" element={<TaskDetailPage />} />
          <Route path="tasks" element={<TasksPage />} />
          <Route path="appointments" element={<AppointmentsPage />} />
          {/* Technical Support ticket routes */}
          <Route path="tickets" element={<TicketsQueuePage />} />
          <Route path="tickets/:ticketId" element={<TicketSupportDetailPage />} />
        </Route>

        {/* Customer Portal */}
        <Route
          path="/portal"
          element={
            <ProtectedRoute requireRole="Customer">
              <CustomerLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Navigate to="dashboard" replace />} />
          <Route path="dashboard" element={<CustomerDashboardPage />} />
          <Route path="requests/new" element={<NewRequestPage />} />
          <Route path="requests/:requestId" element={<RequestDetailPage />} />
          <Route path="payments" element={<PaymentsPage />} />
          <Route path="tickets" element={<TicketsPage />} />
          <Route path="tickets/:ticketId" element={<TicketDetailPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
          <Route path="profile" element={<ProfilePage />} />
        </Route>

        {/* Default redirect */}
        <Route path="/" element={<Navigate to="/staff/login" replace />} />
        <Route path="*" element={<Navigate to="/staff/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;

// Routes to the correct dashboard based on staff role
function BackOfficeDashboardRouter() {
  const user = useAppSelector((s) => s.auth.staffUser);
  if (user?.roleName === 'Technical Support') {
    return <TechSupportDashboardPage />;
  }
  return <BackOfficeDashboardPage />;
}
