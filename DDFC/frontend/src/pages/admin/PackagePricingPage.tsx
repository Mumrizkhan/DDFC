import React, { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchPackages, updatePackage } from '../../store/slices/packagesSlice';
import { Card } from '../../components/ui/Card';
import { Badge } from '../../components/ui/Badge';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { toast } from 'react-toastify';
import type { PlotType, PlotSize, PackageTier, PackageCategory, DesignType } from '../../types';

const CATEGORIES: { value: PackageCategory; label: string }[] = [
  { value: 'HouseDesign',   label: 'House Design' },
  { value: 'InteriorDesign', label: 'Interior Design' },
  { value: 'Supervision',   label: 'Supervision' },
];

const DESIGN_TYPES: { value: DesignType; label: string; desc: string }[] = [
  { value: 'InclusiveDesign', label: 'Inclusive Design', desc: 'Premium bespoke design services, includes interior design' },
  { value: 'ExclusiveDesign', label: 'Exclusive Design', desc: 'Standard design services delivered by DDFC' },
];

const TIERS: PackageTier[] = ['Bronze', 'Silver', 'Gold'];

const PLOT_SIZES_BY_TYPE: Record<PlotType, PlotSize[]> = {
  Residential: ['FiveMarla', 'EightMarla', 'TenMarla', 'OneKanal', 'TwoKanal'],
  Commercial:  ['FourMarla', 'FiveMarla', 'EightMarla', 'TenMarla', 'OneKanal'],
};

const PLOT_SIZES_LABELS: Record<PlotSize, string> = {
  FourMarla:  '4 Marla',
  FiveMarla:  '5 Marla',
  EightMarla: '8 Marla',
  TenMarla:   '10 Marla',
  OneKanal:   '1 Kanal (20 Marla)',
  TwoKanal:   '2 Kanal (40 Marla)',
};

export const PackagePricingPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { packages, loading } = useAppSelector((s) => s.packages);
  const [plotType, setPlotType] = useState<PlotType>('Residential');
  const [category, setCategory] = useState<PackageCategory>('HouseDesign');
  const [designType, setDesignType] = useState<DesignType>('ExclusiveDesign');
  const plotSizes = PLOT_SIZES_BY_TYPE[plotType];
  const [editingCell, setEditingCell] = useState<{ packageId: string; field: string } | null>(null);
  const [editValue, setEditValue] = useState('');

  useEffect(() => {
    dispatch(fetchPackages({ plotType, category, designType }));
  }, [dispatch, plotType, category, designType]);

  const getPackage = (size: PlotSize, tier: PackageTier) =>
    packages.find(
      (p) => p.plotSize === size && p.packageTier === tier && p.plotType === plotType
           && p.packageCategory === category && p.designType === designType
    );

  const startEdit = (packageId: string, field: string, value: string) => {
    setEditingCell({ packageId, field });
    setEditValue(value);
  };

  const saveEdit = async () => {
    if (!editingCell) return;
    const { packageId, field } = editingCell;
    const pkg = packages.find((p) => p.id === packageId);
    if (!pkg) return;

    const updated = {
      ...pkg,
      lineItems: pkg.lineItems.map((li) =>
        li.id === field
          ? {
              ...li,
              isFree: editValue === '0' || editValue.toLowerCase() === 'free',
              amountDDFC: parseFloat(editValue) || 0,
            }
          : li
      ),
    };
    await dispatch(updatePackage({ id: packageId, data: updated })).unwrap();
    toast.success('Package updated');
    setEditingCell(null);
  };

  if (loading) return <PageLoader />;

  return (
    <div className="space-y-6">
      {/* ── Header + filters ── */}
      <div className="space-y-3">
        <div className="flex items-start justify-between flex-wrap gap-3">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Package & Pricing Configuration</h1>
            <p className="text-gray-500 text-sm mt-1">
              Configure service fees per design track, plot type, and tier.
            </p>
          </div>
        </div>

        {/* Design track (DDFC vs Exclusive) */}
        <div className="flex gap-3">
          {DESIGN_TYPES.map((dt) => (
            <button
              key={dt.value}
              onClick={() => setDesignType(dt.value)}
              className={`flex-1 max-w-xs text-left px-4 py-3 rounded-xl border-2 transition-all
                ${designType === dt.value
                  ? 'border-blue-600 bg-blue-50'
                  : 'border-gray-200 bg-white hover:bg-gray-50'}`}
            >
              <p className={`font-semibold text-sm ${designType === dt.value ? 'text-blue-700' : 'text-gray-700'}`}>{dt.label}</p>
              <p className="text-xs text-gray-400 mt-0.5">{dt.desc}</p>
            </button>
          ))}
        </div>

        {/* Category + plot type row */}
        <div className="flex gap-2 flex-wrap">
          {CATEGORIES.map((c) => (
            <button
              key={c.value}
              onClick={() => setCategory(c.value)}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors
                ${category === c.value
                  ? 'bg-indigo-600 text-white'
                  : 'bg-white border border-gray-300 text-gray-700 hover:bg-gray-50'}`}
            >
              {c.label}
            </button>
          ))}
          <div className="w-px bg-gray-200 mx-1" />
          {(['Residential', 'Commercial'] as PlotType[]).map((t) => (
            <button
              key={t}
              onClick={() => setPlotType(t)}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors
                ${plotType === t
                  ? 'bg-blue-600 text-white'
                  : 'bg-white border border-gray-300 text-gray-700 hover:bg-gray-50'}`}
            >
              {t}
            </button>
          ))}
        </div>
      </div>

      {TIERS.map((tier) => (
        <Card key={tier} title={`${tier} Tier`}>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100">
                  <th className="text-left font-medium text-gray-500 pb-3 pr-4">Service</th>
                  {plotSizes.map((size) => (
                    <th key={size} className="text-center font-medium text-gray-500 pb-3 px-2">
                      {PLOT_SIZES_LABELS[size]}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {/* Get all unique services from any matching package */}
                {plotSizes.flatMap((s) => getPackage(s, tier)?.lineItems ?? [])
                  .filter((item, idx, arr) => arr.findIndex((i) => i.serviceName === item.serviceName) === idx)
                  .sort((a, b) => a.sortOrder - b.sortOrder)
                  .map((serviceTemplate) => (
                    <tr key={serviceTemplate.serviceName} className="hover:bg-gray-50">
                      <td className="py-3 pr-4 font-medium text-gray-900">
                        {serviceTemplate.serviceName}
                      </td>
                      {plotSizes.map((size) => {
                        const pkg = getPackage(size, tier);
                        const lineItem = pkg?.lineItems.find(
                          (li) => li.serviceName === serviceTemplate.serviceName
                        );
                        const isEditing =
                          editingCell?.packageId === pkg?.id &&
                          editingCell?.field === lineItem?.id;

                        return (
                          <td key={size} className="py-3 px-2 text-center">
                            {lineItem ? (
                              isEditing ? (
                                <div className="flex items-center gap-1 justify-center">
                                  <input
                                    type="text"
                                    value={editValue}
                                    onChange={(e) => setEditValue(e.target.value)}
                                    className="w-20 border rounded px-2 py-1 text-xs text-center"
                                    onKeyDown={(e) => {
                                      if (e.key === 'Enter') saveEdit();
                                      if (e.key === 'Escape') setEditingCell(null);
                                    }}
                                    autoFocus
                                  />
                                  <button
                                    onClick={saveEdit}
                                    className="text-green-600 text-xs"
                                  >
                                    ✓
                                  </button>
                                </div>
                              ) : (
                                <button
                                  onClick={() =>
                                    pkg &&
                                    startEdit(
                                      pkg.id,
                                      lineItem.id,
                                      lineItem.isFree ? '0' : String(lineItem.amountDDFC)
                                    )
                                  }
                                  className="hover:bg-blue-50 rounded px-2 py-1 transition-colors"
                                >
                                  {lineItem.isFree ? (
                                    <Badge variant="success">Free</Badge>
                                  ) : (
                                    <span className="text-gray-700">
                                      Rs {lineItem.amountDDFC.toLocaleString()}
                                    </span>
                                  )}
                                </button>
                              )
                            ) : (
                              <span className="text-gray-300">—</span>
                            )}
                          </td>
                        );
                      })}
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        </Card>
      ))}
    </div>
  );
};
