import axios from 'axios';

const API_URL = 'https://localhost:7123'; // Make sure this port matches your backend launchSettings.json

export const createDashboard = async (data) => {
  try {
    const response = await axios.post(`${API_URL}/dashboard`, data);
    return response.data;
  } catch (error) {
    console.error('Error creating dashboard:', error);
    throw error;
  }
};

export const createDashboardFromWizard = async (wizardData) => {
  try {
    const response = await axios.post(`${API_URL}/dashboard/wizard`, wizardData);
    return response.data;
  } catch (error) {
    console.error('Error creating dashboard from wizard:', error);
    throw error;
  }
};
