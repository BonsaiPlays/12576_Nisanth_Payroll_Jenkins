import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Divider,
  MenuItem,
  TextField,
  Typography,
  Tooltip,
} from "@mui/material";
import { SnackbarContext } from "../../context/SnackbarProvider";
import React, { useEffect, useState, useMemo, useContext } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import api from "../../api/axiosClient";
import { NumericFormat } from "react-number-format";
import { formatDate } from "../../utils/date";

function PayslipForm() {
  const [sp] = useSearchParams();
  const navigate = useNavigate();
  const empIdFromParams = sp.get("empId");
  const showSnackbar = useContext(SnackbarContext);

  const [employees, setEmployees] = useState([]);
  const [selectedEmp, setSelectedEmp] = useState(empIdFromParams || "");
  const [ctc, setCtc] = useState(null);
  const [ctcChecked, setCtcChecked] = useState(false);

  const [payslips, setPayslips] = useState([]);

  const [year, setYear] = useState("");
  const [month, setMonth] = useState("");

  const [lopDays, setLopDays] = useState(0);
  const [overrideAllowances, setOverrideAllowances] = useState(0);
  const [overrideDeductions, setOverrideDeductions] = useState(0);

  const [overrideError, setOverrideError] = useState("");

  // Load latest CTC from backend if employee was passed in param
  useEffect(() => {
    if (!empIdFromParams) return;
    setSelectedEmp(empIdFromParams);
    api
      .get(`/hr/employees/${empIdFromParams}/latest-ctc`)
      .then((res) => {
        setCtc(res.data);
        setCtcChecked(true);
      })
      .catch(() => {
        setCtc(null);
        setCtcChecked(true);
      });
  }, [empIdFromParams]);

  // Load employee's payslips for the current year (after CTC is loaded)
  useEffect(() => {
    if (!selectedEmp || !ctc) return;
    api
      .get(
        `/hr/payslips?employeeUserId=${selectedEmp}&year=${new Date().getFullYear()}`
      )
      .then((res) => setPayslips(res.data.items || []))
      .catch(() => setPayslips([]));
  }, [selectedEmp, ctc]);

  // Load latest CTC whenever employee is selected (via params OR dropdown)
  useEffect(() => {
    if (!selectedEmp) return;
    api
      .get(`/hr/employees/${selectedEmp}/latest-ctc`)
      .then((res) => {
        setCtc(res.data);
        setCtcChecked(true);
      })
      .catch(() => {
        setCtc(null);
        setCtcChecked(true);
      });
  }, [selectedEmp]);

  // Resolve year value based on CTC year
  useEffect(() => {
    if (!ctc) return;
    const currentYear = new Date().getFullYear();
    const ctcYear = new Date(ctc.effectiveFrom).getFullYear();
    if (ctcYear === currentYear) {
      setYear(currentYear);
    } else {
      setYear("");
    }
  }, [ctc]);

  // Auto compute gross, tax, etc
  const totalAllowances = useMemo(
    () => ctc?.allowances?.reduce((s, a) => s + a.amount, 0) || 0,
    [ctc]
  );
  const totalDeductions = useMemo(
    () => ctc?.deductions?.reduce((s, d) => s + d.amount, 0) || 0,
    [ctc]
  );
  const gross = (ctc?.basic || 0) + (ctc?.hra || 0) + totalAllowances;
  const tax = (gross - totalDeductions) * ((ctc?.taxPercent || 0) / 100);

  const overridesAllowanceValue = overrideAllowances ? +overrideAllowances : 0;
  const overridesDeductionValue = overrideDeductions ? +overrideDeductions : 0;

  const dailySalary = month && gross ? gross / 30 : 0;
  const lopDeduction = lopDays ? dailySalary * +lopDays : 0;

  const baseNetPay = gross - totalDeductions - tax - lopDeduction;
  const netPay = baseNetPay + overridesAllowanceValue - overridesDeductionValue;

  // Handle netPay override error messages
  useEffect(() => {
    if (netPay < 0) {
      setOverrideError("Net Pay cannot go below 0");
    } else if (netPay < baseNetPay) {
      setOverrideError("Overrides reduce Net Pay below base calculation");
    } else {
      setOverrideError("");
    }
  }, [netPay, baseNetPay]);

  // Build list of valid months with disabled logic
  const monthOptions = useMemo(() => {
    if (!ctc) return [];
    const effectiveMonth = new Date(ctc.effectiveFrom).getMonth() + 1;

    // Split payslips by release status
    const releasedMonths = payslips
      .filter((p) => p.isReleased)
      .map((p) => p.month);

    const approvedNotReleased = payslips
      .filter((p) => !p.isReleased && p.status === 1)
      .map((p) => p.month);

    const pendingMonths = payslips
      .filter((p) => !p.isReleased && p.status === 0)
      .map((p) => p.month);

    const rejectedMonths = payslips
      .filter((p) => !p.isReleased && p.status === 2)
      .map((p) => p.month);

    return [...Array(12).keys()].map((m) => {
      const monthNum = m + 1;
      let isDisabled = false;
      let tooltip = "";

      if (monthNum < effectiveMonth) {
        isDisabled = true;
        tooltip = "CTC not effective for this month";
      } else if (releasedMonths.includes(monthNum)) {
        isDisabled = true;
        tooltip = "Payslip already released for this month";
      } else if (approvedNotReleased.includes(monthNum)) {
        isDisabled = true;
        tooltip = "Payslip exists but not released";
      } else if (pendingMonths.includes(monthNum)) {
        isDisabled = false;
        tooltip = "Payslip pending approval";
      } else if (rejectedMonths.includes(monthNum)) {
        isDisabled = true;
        tooltip = "Payslip was rejected";
      }

      const label = new Date(0, monthNum - 1).toLocaleString("default", {
        month: "long",
      });

      return {
        value: monthNum,
        label: `${label} (${monthNum})`,
        disabled: isDisabled,
        tooltip,
      };
    });
  }, [ctc, payslips]);

  //Auto-fill month
  useEffect(() => {
    if (!ctc) return;

    const effectiveMonth = new Date(ctc.effectiveFrom).getMonth() + 1;
    const currentYear = new Date().getFullYear();
    const ctcYear = new Date(ctc.effectiveFrom).getFullYear();

    // Only proceed if CTC is for current year
    if (ctcYear !== currentYear) return;

    // Create a map of all months with their highest status
    const monthStatusMap = {};
    payslips.forEach((p) => {
      const month = p.month;
      // Status priority: released (2) > approved (1) > pending (0)
      const status = p.isReleased ? 2 : p.status ?? p.Status;

      if (!monthStatusMap[month] || status > monthStatusMap[month].status) {
        monthStatusMap[month] = {
          status,
          isReleased: p.isReleased,
        };
      }
    });

    // Find the first month that needs attention
    let monthToSelect = effectiveMonth;
    for (let m = effectiveMonth; m <= 12; m++) {
      const monthData = monthStatusMap[m];

      // If month has no payslip or has pending status
      if (!monthData || monthData.status === 0) {
        monthToSelect = m;
        break;
      }

      // If month is approved but not released
      if (monthData.status === 1 && !monthData.isReleased) {
        monthToSelect = m;
        break;
      }
    }

    // If we didn't find any gaps, go to next month after latest
    if (monthToSelect === effectiveMonth) {
      const allMonths = Object.keys(monthStatusMap).map(Number);
      const latestMonth =
        allMonths.length > 0 ? Math.max(...allMonths) : effectiveMonth - 1;
      monthToSelect = latestMonth + 1;
    }

    // Ensure we don't go beyond December
    monthToSelect = Math.min(monthToSelect, 12);

    // Only update if different from current value
    if (monthToSelect !== month) {
      setMonth(monthToSelect);
    }

    console.log("New Auto-fill decision:", {
      effectiveMonth,
      monthStatusMap,
      selectedMonth: monthToSelect,
    });
  }, [ctc, payslips]);

  // Validate input fields
  const validate = () => {
    if (!year) {
      showSnackbar(
        "Year is required and must match effective CTC year",
        "error"
      );
      return false;
    }
    if (!month || isNaN(month) || +month < 1 || +month > 12) {
      showSnackbar("Please select a valid month", "error");
      return false;
    }
    if (isNaN(lopDays) || +lopDays < 0 || +lopDays > 31) {
      showSnackbar("LOP days must be between 0 and 31", "error");
      return false;
    }
    if (
      overrideAllowances &&
      (isNaN(overrideAllowances) || +overrideAllowances < 0)
    ) {
      showSnackbar("Override Allowances must be non-negative", "error");
      return false;
    }
    if (
      overrideDeductions &&
      (isNaN(overrideDeductions) || +overrideDeductions < 0)
    ) {
      showSnackbar("Override Deductions must be non-negative", "error");
      return false;
    }
    return true;
  };

  // Submit handler
  const submit = async (e) => {
    e.preventDefault();
    if (!validate()) return;
    try {
      await api.post("/hr/payslips", {
        employeeUserId: +selectedEmp,
        year: +year,
        month: +month,
        lopDays: +lopDays,
        overridesAllowances: overrideAllowances ? +overrideAllowances : null,
        overridesDeductions: overrideDeductions ? +overrideDeductions : null,
      });
      showSnackbar("Payslip generated and awaiting approval", "success");
      setTimeout(() => navigate("/"), 1200);
    } catch (err) {
      if (err.response?.status === 409) {
        showSnackbar("Payslip for this month already exists", "error");
      } else if (err.response?.status === 400) {
        showSnackbar(err.response.data || "Bad request", "error");
      } else {
        showSnackbar("Failed to create payslip", "error");
      }
    }
  };

  // return (
  //   <Box sx={{ p: 3 }}>
  //     <Typography variant="h5" gutterBottom>
  //       Generate Payslip
  //     </Typography>

  //     {!empIdFromParams && (
  //       <TextField
  //         select
  //         fullWidth
  //         label="Select Employee with Approved CTC"
  //         value={selectedEmp}
  //         onChange={(e) => setSelectedEmp(e.target.value)}
  //         onFocus={async () => {
  //           try {
  //             const { data } = await api.get("/hr/employees-with-ctc");
  //             setEmployees(data || []);
  //           } catch {
  //             showSnackbar("Failed to load employees list", "error");
  //           }
  //         }}
  //         margin="normal"
  //       >
  //         <MenuItem value="">List of Employees</MenuItem>
  //         {employees.map((emp) => (
  //           <MenuItem key={emp.id} value={emp.id}>
  //             {emp.fullName} ({emp.email}){" "}
  //             {emp.department ? `- ${emp.department}` : ""}
  //           </MenuItem>
  //         ))}
  //       </TextField>
  //     )}

  //     {selectedEmp && (
  //       <>
  //         {ctc && (
  //           <Card sx={{ p: 2, mt: 2 }}>
  //             <Typography variant="subtitle1">Latest Approved CTC</Typography>
  //             <Typography>
  //               <b>Basic:</b> ₹{ctc.basic} | <b>HRA:</b> ₹{ctc.hra}
  //             </Typography>
  //             <Typography>
  //               <b>Gross CTC:</b> ₹{ctc.grossCTC}
  //             </Typography>
  //             <Typography>
  //               <b>Tax %:</b> {ctc.taxPercent}%
  //             </Typography>
  //             {ctc.allowances?.length > 0 && (
  //               <>
  //                 <Typography variant="subtitle2" sx={{ mt: 1 }}>
  //                   Allowances:
  //                 </Typography>
  //                 {ctc.allowances.map((a, i) => (
  //                   <Typography key={i}>
  //                     • {a.label}: ₹{a.amount}
  //                   </Typography>
  //                 ))}
  //               </>
  //             )}
  //             {ctc.deductions?.length > 0 && (
  //               <>
  //                 <Typography variant="subtitle2" sx={{ mt: 1 }}>
  //                   Deductions:
  //                 </Typography>
  //                 {ctc.deductions.map((d, i) => (
  //                   <Typography key={i}>
  //                     • {d.label}: ₹{d.amount}
  //                   </Typography>
  //                 ))}
  //               </>
  //             )}
  //             <Typography sx={{ mt: 1 }}>
  //               <b>Effective From:</b> {formatDate(ctc.effectiveFrom)}
  //             </Typography>
  //           </Card>
  //         )}

  //         {!ctc && ctcChecked && (
  //           <Card sx={{ p: 2, mt: 2 }} variant="outlined">
  //             <Typography color="warning.main">
  //               This employee has no approved CTC — payslip cannot be generated
  //             </Typography>
  //             <Button sx={{ mt: 1 }} onClick={() => navigate(-1)}>
  //               Go Back
  //             </Button>
  //           </Card>
  //         )}
  //       </>
  //     )}

  //     {(() => {
  //       if (!ctc) return null;
  //       const ctcYear = new Date(ctc.effectiveFrom).getFullYear();
  //       const thisYear = new Date().getFullYear();

  //       if (ctcYear > thisYear) {
  //         return (
  //           <Card sx={{ p: 2, mt: 2 }} variant="outlined">
  //             <Typography color="warning.main">
  //               This CTC is for next year. Payslip cannot be generated now.
  //             </Typography>
  //             <Button sx={{ mt: 1 }} onClick={() => navigate(-1)}>
  //               Go Back
  //             </Button>
  //           </Card>
  //         );
  //       }
  //       if (ctcYear < thisYear) {
  //         return (
  //           <Card sx={{ p: 2, mt: 2 }} variant="outlined">
  //             <Typography color="error.main">
  //               This CTC is from a past year and is no longer valid. Please
  //               deprecate it.
  //             </Typography>
  //             <Button sx={{ mt: 1 }} onClick={() => navigate(-1)}>
  //               Go Back
  //             </Button>
  //           </Card>
  //         );
  //       }

  //       return (
  //         <Box component="form" onSubmit={submit} sx={{ mt: 3 }}>
  //           <TextField
  //             type="number"
  //             label="Year"
  //             value={year}
  //             disabled
  //             fullWidth
  //             margin="normal"
  //           />

  //           <TextField
  //             select
  //             fullWidth
  //             label="Month of Payment"
  //             value={month}
  //             onChange={(e) => setMonth(Number(e.target.value))}
  //             margin="normal"
  //           >
  //             {monthOptions.map((m) =>
  //               m.disabled ? (
  //                 <Tooltip
  //                   key={m.value}
  //                   title={m.tooltip || ""}
  //                   arrow
  //                   placement="top"
  //                   disableInteractive
  //                 >
  //                   <span style={{ display: "block", pointerEvents: "auto" }}>
  //                     <MenuItem
  //                       key={m.value}
  //                       value={m.value}
  //                       disabled
  //                       style={{ pointerEvents: "none" }}
  //                       title={m.tooltip}
  //                     >
  //                       {m.label}
  //                     </MenuItem>
  //                   </span>
  //                 </Tooltip>
  //               ) : (
  //                 <MenuItem key={m.value} value={m.value} title={m.tooltip}>
  //                   {m.label}
  //                 </MenuItem>
  //               )
  //             )}
  //           </TextField>

  //           <TextField
  //             type="number"
  //             inputProps={{ min: 0, max: 31, step: 1, pattern: "[0-9]*" }}
  //             label="LOP Days"
  //             value={lopDays}
  //             onChange={(e) => setLopDays(Number(e.target.value))}
  //             fullWidth
  //             margin="normal"
  //             required
  //           />

  //           <NumericFormat
  //             customInput={TextField}
  //             label="Override Total Allowances (Optional)"
  //             fullWidth
  //             margin="normal"
  //             value={overrideAllowances}
  //             thousandSeparator=","
  //             thousandsGroupStyle="lakh"
  //             allowNegative={false}
  //             prefix="₹"
  //             onValueChange={(vals) => {
  //               const val = Number(vals.value || 0);
  //               setOverrideAllowances(val);
  //             }}
  //             error={overrideAllowances > baseNetPay}
  //             helperText={
  //               overrideAllowances > baseNetPay
  //                 ? `Overrides allowances cannot exceed Base Net Pay (₹${baseNetPay.toLocaleString(
  //                     "en-IN"
  //                   )})`
  //                 : ""
  //             }
  //           />

  //           <NumericFormat
  //             customInput={TextField}
  //             label="Override Total Deductions (Optional)"
  //             fullWidth
  //             margin="normal"
  //             value={overrideDeductions}
  //             thousandSeparator=","
  //             thousandsGroupStyle="lakh"
  //             allowNegative={false}
  //             prefix="₹"
  //             onValueChange={(vals) => {
  //               const val = Number(vals.value || 0);
  //               setOverrideDeductions(val);
  //             }}
  //             error={overrideDeductions > baseNetPay}
  //             helperText={
  //               overrideDeductions > baseNetPay
  //                 ? `Overrides deductions cannot exceed Base Net Pay (₹${baseNetPay.toLocaleString(
  //                     "en-IN"
  //                   )})`
  //                 : ""
  //             }
  //           />

  //           <Card sx={{ mt: 2, mb: 2 }}>
  //             <CardHeader title="Payslip Preview" />
  //             <Divider />
  //             <CardContent>
  //               <Typography>
  //                 <b>Year/Month:</b> {year || "—"} / {month || "—"}
  //               </Typography>
  //               <Typography>
  //                 <b>Basic:</b> ₹{ctc.basic}
  //               </Typography>
  //               <Typography>
  //                 <b>HRA:</b> ₹{ctc.hra}
  //               </Typography>
  //               <Typography>
  //                 <b>Total Allowances:</b> ₹{totalAllowances}
  //               </Typography>
  //               <Typography>
  //                 <b>Total Deductions:</b> ₹{totalDeductions}
  //               </Typography>
  //               <Divider sx={{ my: 1 }} />
  //               <Typography>
  //                 <b>Gross:</b> ₹{gross}
  //               </Typography>
  //               <Typography>
  //                 <b>Tax ({ctc.taxPercent}%):</b> ₹{tax.toFixed(2)}
  //               </Typography>
  //               <Typography>
  //                 <b>LOP Days:</b> {lopDays || 0} (≈ -₹{lopDeduction.toFixed(2)}
  //                 )
  //               </Typography>
  //               {overridesAllowanceValue > 0 && (
  //                 <Typography>
  //                   <b>+ Override Allowances:</b> ₹{overridesAllowanceValue}
  //                 </Typography>
  //               )}
  //               {overridesDeductionValue > 0 && (
  //                 <Typography>
  //                   <b>- Override Deductions:</b> ₹{overridesDeductionValue}
  //                 </Typography>
  //               )}
  //               <Typography
  //                 variant="h6"
  //                 color={netPay < baseNetPay ? "error.main" : "success.main"}
  //               >
  //                 Net Pay: ₹{netPay.toLocaleString("en-IN")}
  //               </Typography>
  //               {overrideError && (
  //                 <Typography color="error.main" variant="body2">
  //                   {overrideError}
  //                 </Typography>
  //               )}
  //             </CardContent>
  //           </Card>

  //           <Button
  //             type="submit"
  //             variant="contained"
  //             sx={{ mt: 2 }}
  //             disabled={
  //               overrideAllowances > baseNetPay ||
  //               overrideDeductions > baseNetPay ||
  //               netPay < 0
  //             }
  //           >
  //             Generate Payslip
  //           </Button>
  //         </Box>
  //       );
  //     })()}
  //   </Box>
  // );

  return (
    <Box sx={{ p: 3 }} data-testid="generate-payslip-page">
      <Typography variant="h5" gutterBottom data-testid="page-title">
        Generate Payslip
      </Typography>

      {!empIdFromParams && (
        <TextField
          select
          fullWidth
          label="Select Employee with Approved CTC"
          value={selectedEmp}
          onChange={(e) => setSelectedEmp(e.target.value)}
          onFocus={async () => {
            try {
              const { data } = await api.get("/hr/employees-with-ctc");
              setEmployees(data || []);
            } catch {
              showSnackbar("Failed to load employees list", "error");
            }
          }}
          margin="normal"
          inputProps={{ "data-testid": "employee-select" }}
        >
          <MenuItem value="" data-testid="default-employee-option">
            List of Employees
          </MenuItem>
          {employees.map((emp) => (
            <MenuItem
              key={emp.id}
              value={emp.id}
              data-testid={`employee-option-${emp.id}`}
            >
              {emp.fullName} ({emp.email}){" "}
              {emp.department ? `- ${emp.department}` : ""}
            </MenuItem>
          ))}
        </TextField>
      )}

      {selectedEmp && (
        <>
          {ctc && (
            <Card sx={{ p: 2, mt: 2 }} data-testid="ctc-details-card">
              <Typography variant="subtitle1" data-testid="ctc-title">
                Latest Approved CTC
              </Typography>
              <Typography data-testid="basic-hra-info">
                <b>Basic:</b> ₹{ctc.basic} | <b>HRA:</b> ₹{ctc.hra}
              </Typography>
              <Typography data-testid="gross-ctc-info">
                <b>Gross CTC:</b> ₹{ctc.grossCTC}
              </Typography>
              <Typography data-testid="tax-percent-info">
                <b>Tax %:</b> {ctc.taxPercent}%
              </Typography>
              {ctc.allowances?.length > 0 && (
                <>
                  <Typography
                    variant="subtitle2"
                    sx={{ mt: 1 }}
                    data-testid="allowances-title"
                  >
                    Allowances:
                  </Typography>
                  {ctc.allowances.map((a, i) => (
                    <Typography key={i} data-testid={`allowance-item-${i}`}>
                      • {a.label}: ₹{a.amount}
                    </Typography>
                  ))}
                </>
              )}
              {ctc.deductions?.length > 0 && (
                <>
                  <Typography
                    variant="subtitle2"
                    sx={{ mt: 1 }}
                    data-testid="deductions-title"
                  >
                    Deductions:
                  </Typography>
                  {ctc.deductions.map((d, i) => (
                    <Typography key={i} data-testid={`deduction-item-${i}`}>
                      • {d.label}: ₹{d.amount}
                    </Typography>
                  ))}
                </>
              )}
              <Typography sx={{ mt: 1 }} data-testid="effective-date-info">
                <b>Effective From:</b> {formatDate(ctc.effectiveFrom)}
              </Typography>
            </Card>
          )}

          {!ctc && ctcChecked && (
            <Card
              sx={{ p: 2, mt: 2 }}
              variant="outlined"
              data-testid="no-ctc-warning"
            >
              <Typography color="warning.main" data-testid="warning-message">
                This employee has no approved CTC — payslip cannot be generated
              </Typography>
              <Button
                sx={{ mt: 1 }}
                onClick={() => navigate(-1)}
                data-testid="go-back-button"
              >
                Go Back
              </Button>
            </Card>
          )}
        </>
      )}

      {(() => {
        if (!ctc) return null;
        const ctcYear = new Date(ctc.effectiveFrom).getFullYear();
        const thisYear = new Date().getFullYear();

        if (ctcYear > thisYear) {
          return (
            <Card
              sx={{ p: 2, mt: 2 }}
              variant="outlined"
              data-testid="future-ctc-warning"
            >
              <Typography color="warning.main" data-testid="future-ctc-message">
                This CTC is for next year. Payslip cannot be generated now.
              </Typography>
              <Button
                sx={{ mt: 1 }}
                onClick={() => navigate(-1)}
                data-testid="go-back-button"
              >
                Go Back
              </Button>
            </Card>
          );
        }
        if (ctcYear < thisYear) {
          return (
            <Card
              sx={{ p: 2, mt: 2 }}
              variant="outlined"
              data-testid="expired-ctc-warning"
            >
              <Typography color="error.main" data-testid="expired-ctc-message">
                This CTC is from a past year and is no longer valid. Please
                deprecate it.
              </Typography>
              <Button
                sx={{ mt: 1 }}
                onClick={() => navigate(-1)}
                data-testid="go-back-button"
              >
                Go Back
              </Button>
            </Card>
          );
        }

        return (
          <Box
            component="form"
            onSubmit={submit}
            sx={{ mt: 3 }}
            data-testid="payslip-form"
          >
            <TextField
              type="number"
              label="Year"
              value={year}
              disabled
              fullWidth
              margin="normal"
              inputProps={{ "data-testid": "year-input" }}
            />

            <TextField
              select
              fullWidth
              label="Month of Payment"
              value={month}
              onChange={(e) => setMonth(Number(e.target.value))}
              margin="normal"
              inputProps={{ "data-testid": "month-select" }}
            >
              {monthOptions.map((m) =>
                m.disabled ? (
                  <Tooltip
                    key={m.value}
                    title={m.tooltip || ""}
                    arrow
                    placement="top"
                    disableInteractive
                  >
                    <span style={{ display: "block", pointerEvents: "auto" }}>
                      <MenuItem
                        key={m.value}
                        value={m.value}
                        disabled
                        style={{ pointerEvents: "none" }}
                        title={m.tooltip}
                        data-testid={`month-option-${m.value}-disabled`}
                      >
                        {m.label}
                      </MenuItem>
                    </span>
                  </Tooltip>
                ) : (
                  <MenuItem
                    key={m.value}
                    value={m.value}
                    title={m.tooltip}
                    data-testid={`month-option-${m.value}`}
                  >
                    {m.label}
                  </MenuItem>
                )
              )}
            </TextField>

            <TextField
              type="number"
              inputProps={{
                min: 0,
                max: 31,
                step: 1,
                pattern: "[0-9]*",
                "data-testid": "lop-days-input",
              }}
              label="LOP Days"
              value={lopDays}
              onChange={(e) => setLopDays(Number(e.target.value))}
              fullWidth
              margin="normal"
              required
            />

            <NumericFormat
              customInput={TextField}
              label="Override Total Allowances (Optional)"
              fullWidth
              margin="normal"
              value={overrideAllowances}
              thousandSeparator=","
              thousandsGroupStyle="lakh"
              allowNegative={false}
              prefix="₹"
              onValueChange={(vals) => {
                const val = Number(vals.value || 0);
                setOverrideAllowances(val);
              }}
              error={overrideAllowances > baseNetPay}
              helperText={
                overrideAllowances > baseNetPay
                  ? `Overrides allowances cannot exceed Base Net Pay (₹${baseNetPay.toLocaleString(
                      "en-IN"
                    )})`
                  : ""
              }
              inputProps={{ "data-testid": "override-allowances-input" }}
              FormHelperTextProps={{
                "data-testid": "override-allowances-error",
                role: "alert",
              }}
            />

            <NumericFormat
              customInput={TextField}
              label="Override Total Deductions (Optional)"
              fullWidth
              margin="normal"
              value={overrideDeductions}
              thousandSeparator=","
              thousandsGroupStyle="lakh"
              allowNegative={false}
              prefix="₹"
              onValueChange={(vals) => {
                const val = Number(vals.value || 0);
                setOverrideDeductions(val);
              }}
              error={overrideDeductions > baseNetPay}
              helperText={
                overrideDeductions > baseNetPay
                  ? `Overrides deductions cannot exceed Base Net Pay (₹${baseNetPay.toLocaleString(
                      "en-IN"
                    )})`
                  : ""
              }
              inputProps={{ "data-testid": "override-deductions-input" }}
              FormHelperTextProps={{
                "data-testid": "override-deductions-error",
                role: "alert",
              }}
            />

            <Card sx={{ mt: 2, mb: 2 }} data-testid="payslip-preview-card">
              <CardHeader
                title="Payslip Preview"
                data-testid="payslip-preview-title"
              />
              <Divider data-testid="payslip-preview-divider" />
              <CardContent data-testid="payslip-preview-content">
                <Typography data-testid="year-month-preview">
                  <b>Year/Month:</b> {year || "—"} / {month || "—"}
                </Typography>
                <Typography data-testid="basic-preview">
                  <b>Basic:</b> ₹{ctc.basic}
                </Typography>
                <Typography data-testid="hra-preview">
                  <b>HRA:</b> ₹{ctc.hra}
                </Typography>
                <Typography data-testid="total-allowances-preview">
                  <b>Total Allowances:</b> ₹{totalAllowances}
                </Typography>
                <Typography data-testid="total-deductions-preview">
                  <b>Total Deductions:</b> ₹{totalDeductions}
                </Typography>
                <Divider sx={{ my: 1 }} data-testid="preview-divider" />
                <Typography data-testid="gross-preview">
                  <b>Gross:</b> ₹{gross}
                </Typography>
                <Typography data-testid="tax-preview">
                  <b>Tax ({ctc.taxPercent}%):</b> ₹{tax.toFixed(2)}
                </Typography>
                <Typography data-testid="lop-days-preview">
                  <b>LOP Days:</b> {lopDays || 0} (≈ -₹{lopDeduction.toFixed(2)}
                  )
                </Typography>
                {overridesAllowanceValue > 0 && (
                  <Typography data-testid="override-allowances-preview">
                    <b>+ Override Allowances:</b> ₹{overridesAllowanceValue}
                  </Typography>
                )}
                {overridesDeductionValue > 0 && (
                  <Typography data-testid="override-deductions-preview">
                    <b>- Override Deductions:</b> ₹{overridesDeductionValue}
                  </Typography>
                )}
                <Typography
                  variant="h6"
                  color={netPay < baseNetPay ? "error.main" : "success.main"}
                  data-testid="net-pay-preview"
                >
                  Net Pay: ₹{netPay.toLocaleString("en-IN")}
                </Typography>
                {overrideError && (
                  <Typography
                    color="error.main"
                    variant="body2"
                    data-testid="override-error-message"
                  >
                    {overrideError}
                  </Typography>
                )}
              </CardContent>
            </Card>
            <Button
              type="submit"
              variant="contained"
              sx={{ mt: 2 }}
              disabled={
                overrideAllowances > baseNetPay ||
                overrideDeductions > baseNetPay ||
                netPay < 0
              }
              data-testid="generate-payslip-button"
            >
              Generate Payslip
            </Button>
          </Box>
        );
      })()}
    </Box>
  );
}

export default PayslipForm;
