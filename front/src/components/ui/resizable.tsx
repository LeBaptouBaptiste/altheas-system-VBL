'use client';

// Simplified placeholder - resizable panels not used in this project
export function ResizablePanelGroup({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={className}>{children}</div>;
}
export function ResizablePanel({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={className}>{children}</div>;
}
export function ResizableHandle({ className }: { className?: string }) {
  return <div className={className} />;
}
