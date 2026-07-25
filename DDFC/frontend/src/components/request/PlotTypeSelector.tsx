import React from 'react';

const PLOT_TYPES = ['Residential', 'Commercial'] as const;
export type PlotType = (typeof PLOT_TYPES)[number];

interface PlotTypeSelectorProps {
  selectedPlotType: string;
  onChange: (type: PlotType) => void;
  error?: string;
}

export const PlotTypeSelector: React.FC<PlotTypeSelectorProps> = ({
  selectedPlotType,
  onChange,
  error,
}) => (
  <div>
    <label className="block text-sm font-medium text-gray-700 mb-2">
      Plot Type <span className="text-red-500">*</span>
    </label>
    <div className="flex flex-wrap gap-3">
      {PLOT_TYPES.map((type) => (
        <label
          key={type}
          className={`cursor-pointer select-none px-4 py-2 rounded-full border text-sm font-medium transition-colors ${
            selectedPlotType === type
              ? 'bg-blue-600 text-white border-blue-600'
              : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400 hover:text-blue-600'
          }`}
        >
          <input
            type="radio"
            className="sr-only"
            value={type}
            checked={selectedPlotType === type}
            onChange={() => onChange(type)}
          />
          {type}
        </label>
      ))}
    </div>
    {error && <p className="text-red-500 text-sm mt-1">{error}</p>}
  </div>
);
