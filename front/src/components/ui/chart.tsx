'use client';

// Re-export recharts components directly - simpler than the shadcn wrapper
// which has type compatibility issues with newer recharts versions
export {
  BarChart,
  Bar,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
