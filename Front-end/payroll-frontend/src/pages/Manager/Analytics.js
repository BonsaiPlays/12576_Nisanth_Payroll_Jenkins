import React, { useState, useEffect, useContext } from 'react';
import api from '../../api/axiosClient';
import { Bar } from 'react-chartjs-2';
import { SnackbarContext } from "../../context/SnackbarProvider";

import {
  Box,
  Typography,
  Paper,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from '@mui/material';

import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  Title,
  Tooltip,
  Legend
);

function Analytics() {
  const showSnackbar = useContext(SnackbarContext);
  const [summary, setSummary] = useState([]);
  const [compare, setCompare] = useState(null);
  const [anomalies, setAnomalies] = useState([]);
  const [loading, setLoading] = useState(true);

  const year = 2024, month = 6;

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [summaryRes, compareRes, anomaliesRes] = await Promise.all([
          api.get(`/hr-manager/analytics/monthly-summary?year=${year}&month=${month}`),
          api.get(`/hr-manager/analytics/compare?year1=2024&month1=5&year2=2024&month2=${month}`),
          api.get(`/hr-manager/analytics/anomalies?year=${year}&month=${month}&thresholdPercent=20`),
        ]);
        setSummary(summaryRes.data);
        setCompare(compareRes.data);
        setAnomalies(anomaliesRes.data);
      } catch {
        showSnackbar("Failed to load analytics data", "error");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [year, month]);

  const deptChart = {
    labels: summary.map(s => s.department),
    datasets: [
      {
        label: "Net Pay",
        data: summary.map(s => s.totalNet),
        backgroundColor: "rgba(54, 162, 235, 0.6)"
      }
    ]
  };

  return (
    <Box sx={{ p: 3, display: "flex", flexDirection: "column", gap: 3 }}>
      <Typography variant="h5">Payroll Analytics</Typography>

      {loading ? (
        <Box display="flex" justifyContent="center" mt={3}>
          <CircularProgress />
        </Box>
      ) : (
        <>
          {/* Monthly Summary */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6">Monthly Summary</Typography>
            {summary.length > 0 ? (
              <Bar data={deptChart} />
            ) : (
              <Typography color="text.secondary">No summary data found</Typography>
            )}
          </Paper>

          {/* Month Comparison */}
          {compare && (
            <Paper sx={{ p: 2 }}>
              <Typography variant="h6">Compare Months</Typography>
              <Typography>
                <b>Period A:</b> {compare.periodA.month1}/{compare.periodA.year1} — Total Net: {compare.periodA.totalNet}
              </Typography>
              <Typography>
                <b>Period B:</b> {compare.periodB.month2}/{compare.periodB.year2} — Total Net: {compare.periodB.totalNet}
              </Typography>
              <Typography>
                <b>Change:</b> {compare.percentChange}%
              </Typography>
            </Paper>
          )}

          {/* Anomalies */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6">Anomalies</Typography>
            {anomalies.length > 0 ? (
              <TableContainer>
                <Table stickyHeader>
                  <TableHead>
                    <TableRow>
                      <TableCell>Payslip ID</TableCell>
                      <TableCell>Employee Profile ID</TableCell>
                      <TableCell>% Change</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {anomalies.map(a => (
                      <TableRow
                        key={a.id}
                        sx={{
                          backgroundColor: a.isAnomaly
                            ? "rgba(255, 0, 0, 0.1)"
                            : "inherit"
                        }}
                      >
                        <TableCell>{a.id}</TableCell>
                        <TableCell>{a.employeeProfileId}</TableCell>
                        <TableCell>{a.changePercent}%</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            ) : (
              <Typography color="text.secondary">
                No anomalies detected for this month
              </Typography>
            )}
          </Paper>
        </>
      )}
    </Box>
  );
}

export default Analytics;