import type React from 'react';
import { Icon } from './Icon';

export type AdminOption = string | { label: string; value: string | number };

function optionValue(option: AdminOption) {
  return typeof option === 'string' ? option : option.value;
}

function optionLabel(option: AdminOption) {
  return typeof option === 'string' ? option : option.label;
}

export function AdminButton({
  children,
  variant = 'default',
  size = 'md',
  className = '',
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'default' | 'primary' | 'danger' | 'ghost' | 'icon';
  size?: 'sm' | 'md' | 'lg';
}) {
  return (
    <button className={`admin-btn admin-btn-${variant} admin-btn-${size} ${className}`.trim()} {...props}>
      {children}
    </button>
  );
}

export function AdminIconButton({
  icon,
  label,
  className = '',
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & { icon: string; label: string }) {
  return (
    <AdminButton variant="icon" className={className} aria-label={label} title={label} {...props}>
      <Icon name={icon} />
    </AdminButton>
  );
}

export function AdminTextBox({
  label,
  wrapperClassName = '',
  className = '',
  ...props
}: React.InputHTMLAttributes<HTMLInputElement> & { label?: string; wrapperClassName?: string }) {
  const input = <input className={`admin-input ${className}`.trim()} {...props} />;
  if (!label) return input;
  return (
    <label className={`admin-field ${wrapperClassName}`.trim()}>
      <span>{label}</span>
      {input}
    </label>
  );
}

export function AdminSearchBox(props: React.InputHTMLAttributes<HTMLInputElement> & { label?: string }) {
  return <AdminTextBox type="search" {...props} />;
}

export function AdminTextArea({
  label,
  wrapperClassName = '',
  className = '',
  ...props
}: React.TextareaHTMLAttributes<HTMLTextAreaElement> & { label?: string; wrapperClassName?: string }) {
  const input = <textarea className={`admin-textarea ${className}`.trim()} {...props} />;
  if (!label) return input;
  return (
    <label className={`admin-field ${wrapperClassName}`.trim()}>
      <span>{label}</span>
      {input}
    </label>
  );
}

export function AdminSelect({
  label,
  options,
  children,
  wrapperClassName = '',
  className = '',
  ...props
}: React.SelectHTMLAttributes<HTMLSelectElement> & {
  label?: string;
  options?: AdminOption[];
  wrapperClassName?: string;
}) {
  const input = (
    <select className={`admin-select ${className}`.trim()} {...props}>
      {options?.map((option) => (
        <option key={String(optionValue(option))} value={optionValue(option)}>
          {optionLabel(option)}
        </option>
      ))}
      {children}
    </select>
  );
  if (!label) return input;
  return (
    <label className={`admin-field ${wrapperClassName}`.trim()}>
      <span>{label}</span>
      {input}
    </label>
  );
}

export function AdminCheckbox({
  label,
  className = '',
  ...props
}: Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> & { label: React.ReactNode }) {
  return (
    <label className={`admin-checkbox ${className}`.trim()}>
      <input type="checkbox" {...props} />
      <span>{label}</span>
    </label>
  );
}

export function AdminToolbar({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return <div className={`admin-toolbar ${className}`.trim()}>{children}</div>;
}

export function AdminActionGroup({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return <div className={`admin-action-group ${className}`.trim()}>{children}</div>;
}
