import React, { useMemo, useRef, useState } from 'react';
import type { Plot } from '../../types';

interface PlotSelectorProps {
  plots: Plot[];
  loadingPlots: boolean;
  disabled: boolean;
  disabledReason: string;
  error?: string;
  onPlotChange: (plotId: string, plotNumber: string) => void;
}

/**
 * Combobox for selecting an available plot.
 * Use the `key` prop on this component to force a full reset when
 * the phase/sector/plotType changes.
 */
export const PlotSelector: React.FC<PlotSelectorProps> = ({
  plots,
  loadingPlots,
  disabled,
  disabledReason,
  error,
  onPlotChange,
}) => {
  const [plotSearchText, setPlotSearchText] = useState('');
  const [selectedPlotId, setSelectedPlotId] = useState('');
  const [showPlotDropdown, setShowPlotDropdown] = useState(false);

  // Refs to avoid stale closures in the blur setTimeout
  const selectedPlotIdRef = useRef('');
  const plotSearchTextRef = useRef('');
  const plotsRef = useRef(plots);
  plotsRef.current = plots;

  const filteredPlots = useMemo(() => {
    if (!plotSearchText.trim()) return plots;
    const q = plotSearchText.toLowerCase();
    return plots.filter((p) => p.plotNumber.toLowerCase().includes(q));
  }, [plotSearchText, plots]);

  const selectPlot = (plot: Plot) => {
    selectedPlotIdRef.current = plot.id;
    plotSearchTextRef.current = plot.plotNumber;
    setSelectedPlotId(plot.id);
    setPlotSearchText(plot.plotNumber);
    onPlotChange(plot.id, plot.plotNumber);
  };

  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-2">
        Plot No <span className="text-red-500">*</span>
      </label>
      <div className="relative">
        <input
          type="text"
          value={plotSearchText}
          onChange={(e) => {
            const val = e.target.value;
            plotSearchTextRef.current = val;
            selectedPlotIdRef.current = '';
            setPlotSearchText(val);
            setSelectedPlotId('');
            setShowPlotDropdown(true);
          }}
          onFocus={() => setShowPlotDropdown(true)}
          onBlur={() =>
            setTimeout(() => {
              setShowPlotDropdown(false);
              if (!selectedPlotIdRef.current && plotSearchTextRef.current.trim()) {
                const text = plotSearchTextRef.current.trim();
                const pool = plotsRef.current;
                const exact = pool.find((p) => p.plotNumber.toLowerCase() === text.toLowerCase());
                const filtered = pool.filter((p) => p.plotNumber.toLowerCase().includes(text.toLowerCase()));
                const resolved = exact ?? (filtered.length === 1 ? filtered[0] : null);
                if (resolved) selectPlot(resolved);
              }
            }, 150)
          }
          disabled={disabled || loadingPlots}
          placeholder={
            disabled
              ? disabledReason
              : loadingPlots
              ? 'Loading plots...'
              : 'Type to search and select a plot'
          }
          className={`w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 disabled:bg-gray-100 ${
            selectedPlotId
              ? 'border-green-400 focus:ring-green-400 bg-green-50'
              : 'border-gray-300 focus:ring-blue-500'
          }`}
        />
        {showPlotDropdown && filteredPlots.length > 0 && (
          <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-300 rounded-lg shadow-lg z-10 max-h-48 overflow-y-auto">
            {filteredPlots.map((p) => (
              <button
                key={p.id}
                type="button"
                onMouseDown={() => {
                  selectPlot(p);
                  setShowPlotDropdown(false);
                }}
                className="w-full text-left px-4 py-2 hover:bg-blue-50 border-b last:border-b-0 text-sm transition-colors"
              >
                {p.plotNumber}
              </button>
            ))}
          </div>
        )}
        {showPlotDropdown && !disabled && !loadingPlots && filteredPlots.length === 0 && plotSearchText && (
          <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-300 rounded-lg shadow-lg z-10 px-4 py-3">
            <p className="text-sm text-gray-500">No matching plots found</p>
          </div>
        )}
      </div>
      {error && <p className="text-red-500 text-sm mt-1">{error}</p>}
    </div>
  );
};
