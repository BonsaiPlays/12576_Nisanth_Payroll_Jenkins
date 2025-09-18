import '@testing-library/jest-dom';

// Mock date-fns and MUI date pickers
jest.mock('date-fns', () => ({
  ...jest.requireActual('date-fns'),
  format: jest.fn().mockImplementation((date) => date.toISOString()),
}));

jest.mock('@mui/x-date-pickers/LocalizationProvider', () => ({
  LocalizationProvider: ({ children }) => (
    <div data-testid="mock-localization-provider">{children}</div>
  ),
}));

jest.mock('@mui/x-date-pickers/AdapterDateFns', () => ({
  AdapterDateFns: jest.fn().mockImplementation(() => ({
    format: jest.fn().mockImplementation((date) => date.toISOString()),
    parse: jest.fn().mockImplementation((str) => new Date(str)),
  })),
}));

jest.mock('@mui/x-date-pickers/DatePicker', () => {
  return function MockDatePicker(props) {
    return (
      <input
        type="date"
        data-testid="mock-date-picker"
        value={props.value?.toISOString().split('T')[0] || ''}
        onChange={(e) => {
          if (props.onChange) {
            props.onChange(new Date(e.target.value));
          }
        }}
        {...props.slotProps?.textField?.inputProps}
      />
    );
  };
});