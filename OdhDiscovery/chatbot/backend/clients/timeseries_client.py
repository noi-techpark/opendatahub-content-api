"""
Timeseries API Client
Clean, configurable client for ODH Timeseries API
"""
import httpx
import logging
from typing import Any
from datetime import datetime
from config import settings

logger = logging.getLogger(__name__)


class TimeseriesAPIClient:
    """
    Client for ODH Timeseries API
    Designed to be easily configurable and adaptable to API changes
    """

    def __init__(
        self,
        base_url: str | None = None,
        timeout: int | None = None
    ):
        """
        Initialize Timeseries API client

        Args:
            base_url: Override default base URL from settings
            timeout: Override default timeout from settings
        """
        self.base_url = base_url or settings.timeseries_api_base_url
        self.timeout = timeout or settings.api_timeout
        logger.info(f"TimeseriesAPIClient initialized with base_url: {self.base_url}")

    async def get_types(self) -> list[dict]:
        """
        Get list of all available timeseries types

        Returns:
            List of type metadata with sensor counts
        """
        url = f"{self.base_url}/types"

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.get(url)
                response.raise_for_status()
                return response.json()
        except httpx.HTTPError as e:
            logger.error(f"Failed to fetch timeseries types: {e}")
            raise

    async def get_sensors_by_type(
        self,
        type_name: str,
        filter_dict: dict | None = None,
    ) -> list[dict]:
        """
        Get sensors for a specific type

        Args:
            type_name: Type name (e.g., 'temperature', 'parking')
            filter_dict: Filter criteria (e.g., {"active": true})

        Returns:
            List of sensor metadata
        """
        url = f"{self.base_url}/types/{type_name}/sensors"
        params = {}

        if filter_dict:
            # Convert filter dict to query parameters
            # Implementation depends on actual API format
            params["filter"] = str(filter_dict)

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                logger.debug(f"Fetching sensors for type {type_name}")
                response = await client.get(url, params=params)
                response.raise_for_status()
                return response.json()
        except httpx.HTTPError as e:
            logger.error(f"Failed to fetch sensors for type {type_name}: {e}")
            raise

    async def get_timeseries_bulk(
        self,
        sensor_names: list[str],
        from_date: datetime | str | None = None,
        to_date: datetime | str | None = None,
        interval: str | None = None,
    ) -> dict[str, Any]:
        """
        Get timeseries data for multiple sensors in bulk

        Args:
            sensor_names: List of sensor names (entry IDs)
            from_date: Start date/time
            to_date: End date/time
            interval: Aggregation interval (e.g., '1h', '1d')

        Returns:
            Dictionary with sensor_name -> measurements mapping
        """
        url = f"{self.base_url}/sensors/timeseries"

        payload = {
            "sensor_names": sensor_names
        }

        if from_date:
            payload["from"] = from_date if isinstance(from_date, str) else from_date.isoformat()
        if to_date:
            payload["to"] = to_date if isinstance(to_date, str) else to_date.isoformat()
        if interval:
            payload["interval"] = interval

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                logger.debug(f"Fetching bulk timeseries for {len(sensor_names)} sensors")
                response = await client.post(url, json=payload)
                response.raise_for_status()
                return response.json()
        except httpx.HTTPError as e:
            logger.error(f"Failed to fetch bulk timeseries: {e}")
            raise

    async def get_timeseries_single(
        self,
        sensor_name: str,
        from_date: datetime | str | None = None,
        to_date: datetime | str | None = None,
        interval: str | None = None,
    ) -> list[dict]:
        """
        Get timeseries data for a single sensor

        Args:
            sensor_name: Sensor name (entry ID)
            from_date: Start date/time
            to_date: End date/time
            interval: Aggregation interval

        Returns:
            List of measurements
        """
        result = await self.get_timeseries_bulk(
            sensor_names=[sensor_name],
            from_date=from_date,
            to_date=to_date,
            interval=interval
        )
        return result.get(sensor_name, [])

    async def get_latest_measurements(
        self,
        sensor_names: list[str],
    ) -> dict[str, Any]:
        """
        Get latest measurement for each sensor

        Args:
            sensor_names: List of sensor names

        Returns:
            Dictionary with sensor_name -> latest measurement
        """
        url = f"{self.base_url}/sensors/latest"

        payload = {
            "sensor_names": sensor_names
        }

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                logger.debug(f"Fetching latest measurements for {len(sensor_names)} sensors")
                response = await client.post(url, json=payload)
                response.raise_for_status()
                return response.json()
        except httpx.HTTPError as e:
            logger.error(f"Failed to fetch latest measurements: {e}")
            raise

    async def get_sensor_metadata(
        self,
        sensor_name: str,
    ) -> dict[str, Any]:
        """
        Get metadata for a specific sensor

        Args:
            sensor_name: Sensor name

        Returns:
            Sensor metadata object
        """
        url = f"{self.base_url}/sensors/{sensor_name}"

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                logger.debug(f"Fetching metadata for sensor {sensor_name}")
                response = await client.get(url)
                response.raise_for_status()
                return response.json()
        except httpx.HTTPError as e:
            logger.error(f"Failed to fetch metadata for sensor {sensor_name}: {e}")
            raise
