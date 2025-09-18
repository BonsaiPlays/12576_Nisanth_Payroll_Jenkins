/*
As an HR or HR Manager, I want to fill in detailed CTC (Cost to Company) information for employees, 
 so that payroll data is accurate and reflects all salary components. 
*/

import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControlLabel,
  Grid,
  TextField,
  Tooltip,
  Typography,
  FormGroup,
  IconButton,
  Pagination,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
} from "@mui/material";
import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";

import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import { NumericFormat } from "react-number-format";
import api from "../../api/axiosClient";
import { useContext } from "react";
import { SnackbarContext } from "../../context/SnackbarProvider";

import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { AdapterDateFns } from "@mui/x-date-pickers/AdapterDateFns";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import { formatDate } from "../../utils/date";

function CTCForm() {
  const [sp] = useSearchParams();
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [total, setTotalCount] = useState(0);

  const empIdParam = sp.get("empId");
  const empId = empIdParam ? parseInt(empIdParam, 10) : null;
  const [selectedEmployees, setSelectedEmployees] = useState([]);

  const showSnackbar = useContext(SnackbarContext);

  // keep in sync with query param
  useEffect(() => {
    const empIdParam = sp.get("empId");
    if (empIdParam) {
      setSelectedEmployees([parseInt(empIdParam, 10)]);
    }
  }, [sp]);

  useEffect(() => {
    if (!selectedEmployees.length) return;

    api
      .get(`/hr/ctcs?employeeId=${selectedEmployees[0]}`)
      .catch((err) => console.error(err));
  }, [selectedEmployees]);

  const [employees, setEmployees] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [employeeName, setEmployeeName] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);

  const [basic, setBasic] = useState("");
  const [hra, setHra] = useState("");
  const [hraTouched, setHraTouched] = useState(false);
  const [allowances, setAllowances] = useState([]);
  const [deductions, setDeductions] = useState([]);
  const [effectiveFrom, setEffectiveFrom] = useState("");
  const [errors, setErrors] = useState({});
  const [employeeCtcs, setEmployeeCtcs] = useState([]);

  const [batchResults, setBatchResults] = useState([]);
  const [showResultsModal, setShowResultsModal] = useState(false);

  // Load employees
  useEffect(() => {
    const fetchEmployees = async () => {
      try {
        const res = await api.get(
          `/hr/employees?page=${page}&pageSize=${pageSize}&search=${encodeURIComponent(
            searchTerm
          )}`
        );

        setEmployees(res.data.items || []);
        setTotalCount(res.data.total || 0);

        // If we're in single-employee mode (empId query param), hydrate employee name
        if (empId && (res.data.items || []).length > 0) {
          const match = res.data.items.find((e) => e.id === empId);
          if (match) setEmployeeName(match.fullName);
        }
      } catch {
        showSnackbar("Failed loading employees", "error");
      }
    };

    fetchEmployees();
  }, [page, pageSize, empId, searchTerm]);

  const toggleEmployee = (id) => {
    id = Number(id);
    if (selectedEmployees.includes(id)) {
      setSelectedEmployees(selectedEmployees.filter((eid) => eid !== id));
    } else {
      setSelectedEmployees([...selectedEmployees, id]);
    }
  };

  // HRA live update (only when not manually overridden)
  useEffect(() => {
    if (basic && !isNaN(basic)) {
      if (!hraTouched) {
        const autoHra = Math.floor(+basic * 0.4);
        setHra(autoHra.toString());
      }
    } else {
      setHra("");
      setHraTouched(false);
    }
  }, [basic, hraTouched]);

  // Real-time validation: HRA must be >= 0 and less than Basic
  useEffect(() => {
    if (hra === "" || isNaN(hra)) {
      setErrors((prev) => {
        const { hra, ...rest } = prev;
        return rest;
      });
      return;
    }

    if (+hra < 0) {
      setErrors((prev) => ({
        ...prev,
        hra: "HRA cannot be negative",
      }));
    } else if (+hra >= +basic && +basic > 0) {
      setErrors((prev) => ({
        ...prev,
        hra: "HRA must be less than Basic",
      }));
    } else {
      setErrors((prev) => {
        const { hra, ...rest } = prev;
        return rest;
      });
    }
  }, [hra, basic]);

  //Real time validation for allowances and deductions
  const addAllowance = () => {
    setAllowances([...allowances, { label: "", amount: "" }]);

    // Clear any existing errors for the new allowance
    setErrors((prev) => {
      const newErrors = { ...prev };
      delete newErrors[`allowance_label_${allowances.length}`];
      delete newErrors[`allowance_amount_${allowances.length}`];
      return newErrors;
    });
  };

  const addDeduction = () => {
    setDeductions([...deductions, { label: "", amount: "" }]);

    // Clear any existing errors for the new deduction
    setErrors((prev) => {
      const newErrors = { ...prev };
      delete newErrors[`deduction_label_${deductions.length}`];
      delete newErrors[`deduction_amount_${deductions.length}`];
      return newErrors;
    });
  };

  const removeAllowance = (index) => {
    const copy = [...allowances];
    copy.splice(index, 1);
    setAllowances(copy);

    setErrors((prev) => {
      const newErrors = { ...prev };
      delete newErrors[`allowance_label_${index}`];
      delete newErrors[`allowance_amount_${index}`];

      const fixedErrors = {};
      Object.keys(newErrors).forEach((key) => {
        if (
          key.startsWith("allowance_label_") ||
          key.startsWith("allowance_amount_")
        ) {
          const num = parseInt(key.split("_").pop());
          if (num > index) {
            // shift index down by one
            const newKey = key.includes("label")
              ? `allowance_label_${num - 1}`
              : `allowance_amount_${num - 1}`;
            fixedErrors[newKey] = newErrors[key];
          } else {
            fixedErrors[key] = newErrors[key];
          }
        } else {
          fixedErrors[key] = newErrors[key];
        }
      });

      return fixedErrors;
    });
  };

  const removeDeduction = (index) => {
    const copy = [...deductions];
    copy.splice(index, 1);
    setDeductions(copy);

    setErrors((prev) => {
      const newErrors = { ...prev };
      delete newErrors[`deduction_label_${index}`];
      delete newErrors[`deduction_amount_${index}`];

      const fixedErrors = {};
      Object.keys(newErrors).forEach((key) => {
        if (
          key.startsWith("deduction_label_") ||
          key.startsWith("deduction_amount_")
        ) {
          const num = parseInt(key.split("_").pop());
          if (num > index) {
            const newKey = key.includes("label")
              ? `deduction_label_${num - 1}`
              : `deduction_amount_${num - 1}`;
            fixedErrors[newKey] = newErrors[key];
          } else {
            fixedErrors[key] = newErrors[key];
          }
        } else {
          fixedErrors[key] = newErrors[key];
        }
      });

      return fixedErrors;
    });
  };

  const formatINR = (value) => {
    if (value === "" || value === null || isNaN(value)) return "₹0";
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
    }).format(value);
  };

  // Fetch CTCs for selected employees (single & batch)
  useEffect(() => {
    if (!selectedEmployees.length) return;

    const fetchCtcs = async () => {
      try {
        if (selectedEmployees.length === 1) {
          const id = selectedEmployees[0];
          const res = await api.get(`/hr/ctcs?employeeId=${id}`);
          const ctcs = (res.data.items || [])
            .filter((item) => !!item.effectiveFrom)
            .map((item) => ({
              employeeId: id,
              effectiveFrom: String(item.effectiveFrom),
              status: Number(item.status),
            }));
          setEmployeeCtcs([{ employeeId: id, ctcs }]);

          setEmployeeCtcs([{ employeeId: id, ctcs }]);
        } else {
          const results = await Promise.all(
            selectedEmployees.map(async (id) => {
              const res = await api.get(`/hr/ctcs?employeeId=${id}`);
              return {
                employeeId: id,
                ctcs: (res.data.items || [])
                  .filter((item) => !!item.effectiveFrom)
                  .map((item) => ({
                    effectiveFrom: String(item.effectiveFrom),
                    status: Number(item.status),
                  })),
              };
            })
          );
          setEmployeeCtcs(results);
          setEmployeeCtcs(results);
        }
      } catch (err) {
        console.error("CTC fetch error:", err);
        showSnackbar("Failed loading employee CTCs", "error");
      }
    };

    fetchCtcs();
  }, [selectedEmployees]);

  // Yearly Uniqueness Validation
  useEffect(() => {
    if (!effectiveFrom || !employeeCtcs.length) return;

    const selectedYear = effectiveFrom.split("-")[0]; // from input field -> "2025"

    const conflicts = employeeCtcs.filter((emp) =>
      (emp.ctcs || []).some((c) => {
        if (!c?.effectiveFrom) return false;
        const apiYear = c.effectiveFrom.split("-")[0]; // from API string "2025-09-26T00:00:00"
        return c.status === 1 && apiYear === selectedYear;
      })
    );

    if (conflicts.length) {
      const employeeMap = Object.fromEntries(
        employees
          .filter((e) => selectedEmployees.includes(e.id))
          .map((e) => [e.id, e.fullName])
      );

      const names = conflicts
        .map((c) => employeeMap[c.employeeId] || `#${c.employeeId}`)
        .join(", ");

      setErrors((prev) => ({
        ...prev,
        effectiveFrom: `⚠️ ${names} already has a CTC in ${selectedYear}.`,
      }));
    } else {
      setErrors((prev) => {
        const { effectiveFrom, ...rest } = prev;
        return rest;
      });
    }
  }, [effectiveFrom, employeeCtcs, employees, selectedEmployees]);

  const totalAllowances = allowances.reduce((s, a) => s + (+a.amount || 0), 0);
  const totalDeductions = deductions.reduce((s, d) => s + (+d.amount || 0), 0);
  const gross = (+basic || 0) + (+hra || 0) + totalAllowances;
  const taxPercent = gross >= 1200000 ? 12 : 0;
  const tax = (gross - totalDeductions) * ((+taxPercent || 0) / 100);
  const net = gross - totalDeductions - tax;

  // Allowances cannot exceed basic
  useEffect(() => {
    if (
      (+basic > 0 && totalAllowances >= +basic) ||
      (!basic && totalAllowances > 0)
    ) {
      setErrors((prev) => ({
        ...prev,
        totalAllowances: "Allowances cannot exceed Basic pay",
      }));
    } else {
      setErrors((prev) => {
        const { totalAllowances, ...rest } = prev;
        return rest;
      });
    }
  }, [basic, totalAllowances]);

  // Deductions cannot exceed basic
  useEffect(() => {
    if (
      (+basic > 0 && totalDeductions >= +basic) ||
      (!basic && totalDeductions > 0)
    ) {
      setErrors((prev) => ({
        ...prev,
        totalDeductions: "Deductions cannot exceed Basic pay",
      }));
    } else {
      setErrors((prev) => {
        const { totalDeductions, ...rest } = prev;
        return rest;
      });
    }
  }, [basic, totalDeductions]);

  //Allows and deducts Validation
  useEffect(() => {
    const errs = {};

    // Validate allowances
    allowances.forEach((a, i) => {
      if (!a.label.trim()) {
        errs[`allowance_label_${i}`] = "Label required";
      } else {
        // Clear label error if valid
        delete errs[`allowance_label_${i}`];
      }

      if (a.amount === "" || isNaN(a.amount) || +a.amount < 0) {
        errs[`allowance_amount_${i}`] = "Invalid amount";
      } else {
        // Clear amount error if valid
        delete errs[`allowance_amount_${i}`];
      }
    });

    // Validate deductions
    deductions.forEach((d, i) => {
      if (!d.label.trim()) {
        errs[`deduction_label_${i}`] = "Label required";
      } else {
        // Clear label error if valid
        delete errs[`deduction_label_${i}`];
      }

      if (d.amount === "" || isNaN(d.amount) || +d.amount < 0) {
        errs[`deduction_amount_${i}`] = "Invalid amount";
      } else {
        // Clear amount error if valid
        delete errs[`deduction_amount_${i}`];
      }
    });

    setErrors((prev) => {
      // First, clear all existing allowance/deduction errors
      const newErrors = Object.keys(prev).reduce((acc, key) => {
        if (!key.startsWith("allowance_") && !key.startsWith("deduction_")) {
          acc[key] = prev[key];
        }
        return acc;
      }, {});

      // Then merge with new errors
      return { ...newErrors, ...errs };
    });
  }, [allowances, deductions]);

  const validate = () => {
    const errs = {};
    if (!basic.trim() || isNaN(basic) || +basic <= 0)
      errs.basic = "Basic must be greater than 0";
    if (!hra.trim() || isNaN(hra) || +hra < 0)
      errs.hra = "HRA must be non-negative";
    if (+hra >= +basic) errs.hra = "HRA must be less than Basic";

    allowances.forEach((a, i) => {
      if (!a.label.trim()) errs[`allowance_label_${i}`] = "Label required";
      if (a.amount === "" || isNaN(a.amount) || +a.amount < 0)
        errs[`allowance_amount_${i}`] = "Invalid amount";
    });

    deductions.forEach((d, i) => {
      if (!d.label.trim()) errs[`deduction_label_${i}`] = "Label required";
      if (d.amount === "" || isNaN(d.amount) || +d.amount < 0)
        errs[`deduction_amount_${i}`] = "Invalid amount";
    });

    if (+basic > 0 && totalAllowances >= +basic) {
      errs.totalAllowances = "Allowances cannot exceed Basic pay";
    }

    if (+basic > 0 && totalDeductions >= +basic) {
      errs.totalDeductions = "Deductions cannot exceed Basic pay";
    }

    if (!effectiveFrom) errs.effectiveFrom = "Effective From date required";

    if (net < 0) errs._summary = "Net CTC cannot be negative";

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleFormSubmit = (e) => {
    e.preventDefault();
    if (selectedEmployees.length === 0) {
      showSnackbar("Select at least one employee!", "error");
      return;
    }
    if (!validate()) {
      showSnackbar("Fix validation errors before submitting", "error");
      return;
    }
    setShowConfirm(true);
  };

  const handleAllowanceLabelChange = (val, i) => {
    const copy = [...allowances];
    copy[i].label = val;
    setAllowances(copy);

    const labels = copy.map((a) => a.label.toLowerCase().trim());
    if (labels.filter((l) => l === val.toLowerCase().trim()).length > 1) {
      setErrors((prev) => ({
        ...prev,
        [`allowance_label_${i}`]: "Duplicate label not allowed",
      }));
    } else {
      setErrors((prev) => {
        const { [`allowance_label_${i}`]: _, ...rest } = prev;
        return rest;
      });
    }
  };

  const handleDeductionLabelChange = (val, i) => {
    const copy = [...deductions];
    copy[i].label = val;
    setDeductions(copy);

    const labels = copy.map((d) => d.label.toLowerCase().trim());
    if (labels.filter((l) => l === val.toLowerCase().trim()).length > 1) {
      setErrors((prev) => ({
        ...prev,
        [`deduction_label_${i}`]: "Duplicate label not allowed",
      }));
    } else {
      setErrors((prev) => {
        const { [`deduction_label_${i}`]: _, ...rest } = prev;
        return rest;
      });
    }
  };

  const confirmSubmit = async () => {
    try {
      const payload = {
        employeeUserIds: selectedEmployees.map((id) => +id),
        basic: +basic,
        hra: +hra,
        allowanceItems: allowances.map((a) => ({
          label: a.label,
          amount: +a.amount,
        })),
        deductionItems: deductions.map((d) => ({
          label: d.label,
          amount: +d.amount,
        })),
        taxPercent: +taxPercent,
        effectiveFrom: new Date(effectiveFrom),
      };

      if (payload.employeeUserIds.length > 1) {
        const { data } = await api.post(`/hr/ctc/batch`, payload);

        if (data?.results || data?.Results) {
          const results = data.results || data.Results;

          const successes = results.filter((r) => r.status === "Created");
          const conflicts = results.filter((r) => r.status === "Conflict");

          if (successes.length) {
            showSnackbar(
              `CTC created for ${successes.length} employee(s).`,
              "success"
            );
          }
          if (conflicts.length) {
            showSnackbar(
              `${conflicts.length} employee(s) skipped (already have CTC this year).`,
              "warning"
            );
          }
          setBatchResults(results);
          setShowResultsModal(true);
        }
      } else {
        await api.post(`/hr/ctc/${payload.employeeUserIds[0]}`, payload);
        showSnackbar("CTC submitted for approval", "success");
        setTimeout(() => navigate(-1), 1200);
      }

      setShowConfirm(false);
    } catch (err) {
      if (err.response?.data?.error) {
        showSnackbar(err.response.data.error, "error");
      } else {
        showSnackbar("Failed creating CTC", "error");
      }
      setShowConfirm(false);
    }
  };

  const filteredEmployees = employees.filter(
    (e) =>
      e.fullName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      e.email?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const hasErrors = Object.keys(errors).length > 0;
  const isBasicInvalid = !basic || isNaN(basic) || Number(basic) <= 0;
  const isDisabled = hasErrors || isBasicInvalid;

  return (
    <Box
      sx={{ p: 3, display: "flex", flexDirection: "column" }}
      data-testid="ctc-form-container"
    >
      <Typography variant="h5" gutterBottom data-testid="ctc-form-title">
        {empId
          ? `Create CTC for ${employeeName || `Employee #${empId}`}`
          : "Apply CTC to Employees"}
      </Typography>

      {!empId && (
        <Box mb={2} data-testid="employee-selection-section">
          <Button
            variant="outlined"
            onClick={() => setShowModal(true)}
            data-testid="select-employees-button"
          >
            Select Employees
          </Button>
          {selectedEmployees.length > 0 && (
            <Typography
              variant="body2"
              color="info.main"
              component="span"
              sx={{ ml: 2 }}
              data-testid="selected-employees-count"
            >
              {selectedEmployees.length} selected
            </Typography>
          )}
        </Box>
      )}

      {/* Form Section */}
      <Box component="form" onSubmit={handleFormSubmit} data-testid="ctc-form">
        <NumericFormat
          customInput={TextField}
          label="Basic"
          fullWidth
          margin="normal"
          value={basic}
          thousandSeparator=","
          thousandsGroupStyle="lakh"
          allowNegative={false}
          prefix="₹"
          onValueChange={(vals) => setBasic(vals.value)}
          error={!!errors.basic}
          helperText={errors.basic}
          inputProps={{ "data-testid": "basic-input" }}
          FormHelperTextProps={{
            "data-testid": "basic-error",
            role: "alert",
          }}
        />

        <NumericFormat
          customInput={TextField}
          label="HRA"
          fullWidth
          margin="normal"
          value={hra}
          thousandSeparator=","
          thousandsGroupStyle="lakh"
          allowNegative={false}
          prefix="₹"
          onValueChange={(vals, sourceInfo) => {
            setHra(vals.value);
            if (sourceInfo?.source === "event") {
              setHraTouched(true);
            }
          }}
          error={!!errors.hra}
          helperText={errors.hra}
          disabled={!basic}
          inputProps={{ "data-testid": "hra-input" }}
          FormHelperTextProps={{
            "data-testid": "hra-error",
            role: "alert",
          }}
        />

        <LocalizationProvider dateAdapter={AdapterDateFns}>
          <DatePicker
            label="Effective From"
            value={effectiveFrom ? new Date(effectiveFrom) : null}
            onChange={(value) => {
              if (value) setEffectiveFrom(value.toISOString().split("T")[0]);
            }}
            slotProps={{
              textField: {
                fullWidth: true,
                margin: "normal",
                error: !!errors.effectiveFrom,
                helperText: errors.effectiveFrom,
                inputProps: { "data-testid": "effective-date-input" },
                FormHelperTextProps: {
                  "data-testid": "effective-date-error",
                  role: "alert",
                },
              },
            }}
            disablePast
          />
        </LocalizationProvider>

        <Typography variant="h6" mt={2} data-testid="allowances-section-title">
          Allowances
        </Typography>
        {errors.totalAllowances && (
          <Typography
            color="error"
            variant="body2"
            data-testid="allowances-error"
          >
            {errors.totalAllowances}
          </Typography>
        )}
        {allowances.map((a, i) => (
          <Grid
            container
            spacing={2}
            alignItems="center"
            key={i}
            mb={1}
            data-testid={`allowance-row-${i}`}
          >
            <Grid item xs={5}>
              <TextField
                placeholder="Label"
                fullWidth
                value={a.label}
                onChange={(e) => handleAllowanceLabelChange(e.target.value, i)}
                error={!!errors[`allowance_label_${i}`]}
                helperText={errors[`allowance_label_${i}`]}
                inputProps={{ "data-testid": `allowance-label-${i}` }}
                FormHelperTextProps={{
                  "data-testid": `allowance-label-error-${i}`,
                  role: "alert",
                }}
              />
            </Grid>
            <Grid item xs={5}>
              <NumericFormat
                customInput={TextField}
                placeholder="Amount"
                fullWidth
                value={a.amount}
                thousandSeparator=","
                thousandsGroupStyle="lakh"
                allowNegative={false}
                prefix="₹"
                onValueChange={(vals) => {
                  const copy = [...allowances];
                  copy[i].amount = vals.value;
                  setAllowances(copy);
                }}
                error={!!errors[`allowance_amount_${i}`]}
                helperText={errors[`allowance_amount_${i}`]}
                inputProps={{ "data-testid": `allowance-amount-${i}` }}
                FormHelperTextProps={{
                  "data-testid": `allowance-amount-error-${i}`,
                  role: "alert",
                }}
              />
            </Grid>
            <Grid item xs={2}>
              <IconButton
                data-testid={`delete-allowance-${i}`}
                color="error"
                onClick={() => removeAllowance(i)}
              >
                <DeleteIcon />
              </IconButton>
            </Grid>
          </Grid>
        ))}
        <Tooltip title={!basic ? "Enter Basic Pay first" : "Add Allowance"}>
          <span>
            <Button
              startIcon={<AddIcon />}
              onClick={addAllowance}
              disabled={!basic}
              data-testid="add-allowance-button"
            >
              Add Allowance
            </Button>
          </span>
        </Tooltip>

        <Typography variant="h6" mt={2} data-testid="deductions-section-title">
          Deductions
        </Typography>
        {errors.totalDeductions && (
          <Typography
            color="error"
            variant="body2"
            data-testid="deductions-error"
          >
            {errors.totalDeductions}
          </Typography>
        )}
        {deductions.map((d, i) => (
          <Grid
            container
            spacing={2}
            alignItems="center"
            key={i}
            mb={1}
            data-testid={`deduction-row-${i}`}
          >
            <Grid item xs={5}>
              <TextField
                placeholder="Label"
                fullWidth
                value={d.label}
                onChange={(e) => handleDeductionLabelChange(e.target.value, i)}
                error={!!errors[`deduction_label_${i}`]}
                helperText={errors[`deduction_label_${i}`]}
                inputProps={{ "data-testid": `deduction-label-${i}` }}
                FormHelperTextProps={{
                  "data-testid": `deduction-label-error-${i}`,
                  role: "alert",
                }}
              />
            </Grid>
            <Grid item xs={5}>
              <NumericFormat
                customInput={TextField}
                placeholder="Amount"
                fullWidth
                value={d.amount}
                thousandSeparator=","
                thousandsGroupStyle="lakh"
                allowNegative={false}
                prefix="₹"
                onValueChange={(vals) => {
                  const copy = [...deductions];
                  copy[i].amount = vals.value;
                  setDeductions(copy);
                }}
                error={!!errors[`deduction_amount_${i}`]}
                helperText={errors[`deduction_amount_${i}`]}
                inputProps={{ "data-testid": `deduction-amount-${i}` }}
                FormHelperTextProps={{
                  "data-testid": `deduction-amount-error-${i}`,
                  role: "alert",
                }}
              />
            </Grid>
            <Grid item xs={2}>
              <IconButton
                color="error"
                onClick={() => removeDeduction(i)}
                data-testid={`delete-deduction-${i}`}
              >
                <DeleteIcon />
              </IconButton>
            </Grid>
          </Grid>
        ))}
        <Tooltip title={!basic ? "Enter Basic Pay first" : "Add Deduction"}>
          <span>
            <Button
              startIcon={<AddIcon />}
              onClick={addDeduction}
              disabled={!basic}
              data-testid="add-deduction-button"
            >
              Add Deduction
            </Button>
          </span>
        </Tooltip>

        <TextField
          label="Tax Percent"
          fullWidth
          margin="normal"
          value={
            gross <= 400000
              ? "0 %"
              : gross <= 800000
              ? "5 %"
              : gross <= 1200000
              ? "10 %"
              : gross <= 1600000
              ? "15 %"
              : gross <= 2000000
              ? "20 %"
              : gross <= 2400000
              ? "25 %"
              : "30 %"
          }
          InputProps={{
            readOnly: true,
          }}
          disabled
          inputProps={{ "data-testid": "tax-percent-display" }}
          helperText={
            gross <= 400000
              ? "No tax applied up to ₹4L"
              : `Tax slab applied based on ₹${(gross / 100000).toFixed(
                  2
                )}L income`
          }
        />

        <Card sx={{ mt: 2, mb: 2 }} data-testid="ctc-preview-card">
          <CardHeader title="CTC Preview" data-testid="ctc-preview-title" />
          <Divider data-testid="ctc-preview-divider" />
          <CardContent data-testid="ctc-preview-content">
            <Typography data-testid="basic-preview">
              <b>Basic:</b> {formatINR(basic || 0)}
            </Typography>
            <Typography data-testid="hra-preview">
              <b>HRA:</b> {formatINR(hra || 0)}
            </Typography>
            <Typography data-testid="total-allowances-preview">
              <b>Total Allowances:</b> {formatINR(totalAllowances)}
            </Typography>
            <Typography data-testid="total-deductions-preview">
              <b>Total Deductions:</b> {formatINR(totalDeductions)}
            </Typography>
            <Divider sx={{ my: 1 }} data-testid="ctc-preview-divider-2" />
            <Typography data-testid="gross-ctc-preview">
              <b>Gross CTC:</b> {formatINR(gross)}
            </Typography>
            <Typography data-testid="tax-preview">
              <b>Tax ({taxPercent || 0}%):</b> {formatINR(isNaN(tax) ? 0 : tax)}
            </Typography>
            <Typography
              variant="h6"
              color={net < 0 ? "error.main" : "success.main"}
              data-testid="net-ctc-preview"
            >
              Net CTC: {formatINR(isNaN(net) ? 0 : net)}
            </Typography>
            {effectiveFrom && (
              <Typography
                variant="body2"
                color="text.secondary"
                data-testid="effective-date-preview"
              >
                Effective From: {formatDate(effectiveFrom)}
              </Typography>
            )}
            {errors._summary && (
              <Typography color="error" data-testid="summary-error">
                {errors._summary}
              </Typography>
            )}
          </CardContent>
        </Card>

        <Button
          type="submit"
          variant="contained"
          sx={{ mt: 2 }}
          disabled={isDisabled}
          data-testid="submit-ctc-button"
        >
          Apply CTC
        </Button>
      </Box>

      {/* Employee Selection Modal */}
      <Dialog
        fullWidth
        maxWidth="md"
        open={showModal}
        onClose={() => setShowModal(false)}
        data-testid="employee-selection-modal"
      >
        <DialogTitle data-testid="employee-selection-modal-title">
          Select Employees
        </DialogTitle>
        <DialogContent data-testid="employee-selection-modal-content">
          <TextField
            fullWidth
            label="Search employees"
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value);
              setPage(1);
            }}
            margin="normal"
            inputProps={{ "data-testid": "employee-search-input" }}
          />
          <Box
            sx={{ maxHeight: 400, overflowY: "auto" }}
            data-testid="employee-list-container"
          >
            <FormGroup data-testid="employee-checkbox-group">
              {employees.map((emp) => (
                <FormControlLabel
                  key={emp.id}
                  control={
                    <Checkbox
                      checked={selectedEmployees.includes(emp.id)}
                      onChange={() => toggleEmployee(emp.id)}
                      data-testid={`employee-checkbox-${emp.id}`}
                    />
                  }
                  label={`${emp.fullName} (${emp.email})`}
                  data-testid={`employee-label-${emp.id}`}
                />
              ))}
            </FormGroup>

            {employees.length === 0 && (
              <Typography data-testid="no-employees-message">
                No employees found.
              </Typography>
            )}
          </Box>

          {/* Pagination bar */}
          <Box
            display="flex"
            justifyContent="space-between"
            mt={2}
            data-testid="employee-pagination"
          >
            <Button
              disabled={page === 1}
              onClick={() => setPage((p) => Math.max(p - 1, 1))}
              data-testid="prev-employee-page-button"
            >
              Previous
            </Button>

            <Typography
              variant="body2"
              align="center"
              data-testid="employee-page-info"
            >
              Page {page} of {Math.ceil(total / pageSize) || 1}
            </Typography>

            <Button
              disabled={page >= Math.ceil(total / pageSize)}
              onClick={() => setPage((p) => p + 1)}
              data-testid="next-employee-page-button"
            >
              Next
            </Button>
          </Box>
        </DialogContent>
        <DialogActions data-testid="employee-selection-modal-actions">
          <Button
            onClick={() => setShowModal(false)}
            data-testid="cancel-employee-selection-button"
          >
            Cancel
          </Button>
          <Button
            onClick={() => setShowModal(false)}
            variant="contained"
            data-testid="confirm-employee-selection-button"
          >
            Done
          </Button>
        </DialogActions>
      </Dialog>

      {/* Confirm Submission Modal */}
      <Dialog
        aria-labelledby="confirm-ctc-title"
        open={showConfirm}
        onClose={() => setShowConfirm(false)}
        data-testid="confirm-submission-modal"
      >
        <DialogTitle data-testid="confirm-submission-title">
          Confirm CTC Submission
        </DialogTitle>
        <DialogContent data-testid="confirm-submission-content">
          <Typography data-testid="confirm-submission-message">
            Are you sure you want to submit this CTC for{" "}
            {selectedEmployees.length} employee(s)?
          </Typography>
        </DialogContent>
        <DialogActions data-testid="confirm-submission-actions">
          <Button
            onClick={() => setShowConfirm(false)}
            data-testid="cancel-submission-button"
          >
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={confirmSubmit}
            data-testid="confirm-submission-button"
          >
            Yes, Submit
          </Button>
        </DialogActions>
      </Dialog>

      {/* Result Display Modal */}
      <Dialog
        open={showResultsModal}
        onClose={() => setShowResultsModal(false)}
        fullWidth
        maxWidth="md"
        data-testid="results-modal"
      >
        <DialogTitle data-testid="results-modal-title">
          Batch CTC Results
        </DialogTitle>
        <DialogContent dividers data-testid="results-modal-content">
          <Table size="small" data-testid="results-table">
            <TableHead data-testid="results-table-header">
              <TableRow data-testid="results-header-row">
                <TableCell data-testid="employee-name-header">
                  Employee
                </TableCell>
                <TableCell data-testid="employee-email-header">Email</TableCell>
                <TableCell data-testid="status-header">Status</TableCell>
                <TableCell data-testid="message-header">Message</TableCell>
              </TableRow>
            </TableHead>
            <TableBody data-testid="results-table-body">
              {batchResults.map((r, idx) => (
                <TableRow key={idx} data-testid={`result-row-${idx}`}>
                  <TableCell data-testid={`employee-name-${idx}`}>
                    {r.employee || `#${r.employeeId}`}
                  </TableCell>
                  <TableCell data-testid={`employee-email-${idx}`}>
                    {r.email || "-"}
                  </TableCell>
                  <TableCell
                    style={{ color: r.status === "Conflict" ? "red" : "green" }}
                    data-testid={`status-${idx}`}
                  >
                    {r.status}
                  </TableCell>
                  <TableCell data-testid={`message-${idx}`}>
                    {r.message}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </DialogContent>
        <DialogActions data-testid="results-modal-actions">
          <Button
            onClick={() => {
              setShowResultsModal(false);
              setTimeout(() => navigate(-1), 1000);
            }}
            data-testid="close-results-modal-button"
          >
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default CTCForm;
