import React, { useEffect } from 'react';
import { useAppSelector } from '../../store/hooks';
import { Card } from '../../components/ui/Card';
import { Download, CreditCard } from 'lucide-react';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { Badge } from '../../components/ui/Badge';
import { format } from 'date-fns';

export const PaymentsPage: React.FC = () => {
  const { requests: myRequests, loading } = useAppSelector((s) => s.requests);

  const allPayments = myRequests.flatMap((r) =>
    (r.payments ?? []).map((p) => ({ ...p, requestId: r.requestId }))
  );

  if (loading) return <PageLoader />;

  return (
    <div className="max-w-3xl mx-auto p-4 space-y-5">
      <h1 className="text-2xl font-bold text-gray-900">Payments & Challans</h1>

      <Card>
        {allPayments.length === 0 ? (
          <div className="text-center py-12 text-gray-400">
            <CreditCard size={36} className="mx-auto mb-2 opacity-30" />
            <p>No payment records found</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100">
                  <th className="text-left pb-3 font-medium text-gray-500">Request</th>
                  <th className="text-left pb-3 font-medium text-gray-500">Challan No</th>
                  <th className="text-left pb-3 font-medium text-gray-500">Amount</th>
                  <th className="text-left pb-3 font-medium text-gray-500">Due Date</th>
                  <th className="text-left pb-3 font-medium text-gray-500">Status</th>
                  <th className="text-left pb-3 font-medium text-gray-500">Download</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {allPayments.map((p) => (
                  <tr key={p.paymentId}>
                    <td className="py-3 font-mono text-xs text-blue-600">{p.requestId}</td>
                    <td className="py-3">{p.challanNumber}</td>
                    <td className="py-3 font-medium">PKR {p.amount.toLocaleString()}</td>
                    <td className="py-3 text-gray-500">
                      {p.dueDate ? format(new Date(p.dueDate), 'dd MMM yyyy') : '—'}
                    </td>
                    <td className="py-3">
                      <Badge variant={p.isPaid ? 'success' : 'warning'} size="sm">
                        {p.isPaid ? 'Paid' : 'Pending'}
                      </Badge>
                    </td>
                    <td className="py-3">
                      {p.challanPdfUrl ? (
                        <a
                          href={p.challanPdfUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="flex items-center gap-1 text-blue-600 hover:underline text-xs"
                        >
                          <Download size={13} /> PDF
                        </a>
                      ) : (
                        <span className="text-gray-300">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
};
