"""
Preprocessing strategies for large datasets
Aggregation, summarization, and statistical analysis
"""
import pandas as pd
import numpy as np
import logging
from typing import Any
from collections import Counter

logger = logging.getLogger(__name__)


def field_projection(
    data: dict,
    fields: list[str]
) -> dict:
    """
    Extract only specified fields from API response

    Args:
        data: API response with 'Items' array
        fields: List of fields to keep

    Returns:
        API response with only specified fields
    """
    if 'Items' not in data or not isinstance(data['Items'], list):
        return data

    from .utils import extract_fields

    projected_items = extract_fields(data['Items'], fields)

    return {
        **data,
        'Items': projected_items,
        '_preprocessing': 'field_projection',
        '_fields': fields
    }


def aggregate_datasets(datasets: list[dict]) -> dict:
    """
    Aggregate dataset metadata from MetaData API into a very compact summary

    Args:
        datasets: List of dataset metadata objects from MetaData API with fields:
                 - Shortname: Dataset name
                 - ApiDescription: Description object with language keys
                 - ApiUrl: Complete API URL
                 - Dataspace: Category (tourism, mobility, etc.)
                 - ApiType: 'content' or 'timeseries'
                 - ApiFilter: Array of default filters

    Returns:
        Extremely compact summary organized by dataspace
    """
    if not datasets:
        return {"total_count": 0, "by_dataspace": {}}

    logger.info(f"Aggregating {len(datasets)} datasets from MetaData API")

    # Organize by dataspace with minimal info
    by_dataspace = {}

    for dataset in datasets:
        shortname = dataset.get("Shortname", "Unknown")
        dataspace = dataset.get("Dataspace", "unknown")
        api_type = dataset.get("ApiType", "unknown")

        # Extract brief English description (first 100 chars)
        description = ""
        api_description = dataset.get("ApiDescription", {})
        if isinstance(api_description, dict):
            full_desc = (
                api_description.get("en") or
                api_description.get("de") or
                api_description.get("it") or
                ""
            )
            # Truncate to first sentence or 100 chars
            description = full_desc.split('.')[0][:100] if full_desc else ""

        # Group by dataspace
        if dataspace not in by_dataspace:
            by_dataspace[dataspace] = []

        by_dataspace[dataspace].append({
            "name": shortname,
            "type": api_type,
            "desc": description
        })

    # Create ultra-compact summary
    summary = {
        "total": len(datasets),
        "dataspaces": by_dataspace,
        "_note": "Use dataset 'name' (Shortname) in get_dataset_entries. Type is 'content' or 'timeseries'."
    }

    logger.debug(f"Datasets aggregation complete: {len(datasets)} datasets across {len(by_dataspace)} dataspaces")
    return summary


def aggregate_dataset_entries(entries: list[dict]) -> dict:
    """
    Aggregate dataset entries into statistical summary

    Args:
        entries: List of entry objects

    Returns:
        Aggregated summary with statistics and samples
    """
    if not entries:
        return {
            'total_count': 0,
            'entries': []
        }

    logger.info(f"Aggregating {len(entries)} dataset entries")

    df = pd.DataFrame(entries)

    summary = {
        'total_count': len(entries),
        '_preprocessing': 'aggregation'
    }

    # Count by common fields
    if 'Active' in df.columns:
        summary['active_count'] = int(df['Active'].sum()) if df['Active'].dtype == bool else 0

    if 'Type' in df.columns:
        summary['by_type'] = dict(Counter(df['Type'].dropna()))

    if 'ContactInfos' in df.columns and not df['ContactInfos'].isna().all():
        # Count entries with contact info
        summary['with_contact_info'] = int(df['ContactInfos'].notna().sum())

    if 'GpsInfo' in df.columns and not df['GpsInfo'].isna().all():
        # Count entries with GPS coordinates
        summary['with_gps'] = int(df['GpsInfo'].notna().sum())

    if 'ImageGallery' in df.columns and not df['ImageGallery'].isna().all():
        summary['with_images'] = int(df['ImageGallery'].notna().sum())

    # Include sample entries (first 3)
    summary['sample_entries'] = entries[:3]

    logger.debug(f"Aggregation complete: {summary.get('total_count')} entries")
    return summary


def summarize_measurements(measurements: dict | list) -> dict:
    """
    Summarize timeseries measurements with statistics

    Args:
        measurements: Dictionary of sensor_name -> measurements or list of measurements

    Returns:
        Summary with statistics per sensor
    """
    # Handle bulk response (dict of sensor -> measurements)
    if isinstance(measurements, dict):
        summaries = {}
        for sensor_name, sensor_data in measurements.items():
            if isinstance(sensor_data, list) and len(sensor_data) > 0:
                summaries[sensor_name] = _summarize_sensor_data(sensor_data)
            else:
                summaries[sensor_name] = {'measurement_count': 0}

        return {
            'sensor_summaries': summaries,
            'total_sensors': len(summaries),
            '_preprocessing': 'measurement_summary'
        }

    # Handle single sensor response (list of measurements)
    elif isinstance(measurements, list):
        return {
            'summary': _summarize_sensor_data(measurements),
            '_preprocessing': 'measurement_summary'
        }

    return measurements


def _summarize_sensor_data(data: list[dict]) -> dict:
    """
    Summarize measurements for a single sensor

    Args:
        data: List of measurement objects

    Returns:
        Statistical summary
    """
    if not data:
        return {'measurement_count': 0}

    # Extract numeric values
    values = []
    timestamps = []

    for measurement in data:
        if 'value' in measurement and isinstance(measurement['value'], (int, float)):
            values.append(measurement['value'])
        if 'timestamp' in measurement:
            timestamps.append(measurement['timestamp'])

    summary = {
        'measurement_count': len(data),
        'sample_measurements': data[:5]  # First 5 measurements
    }

    if values:
        summary['statistics'] = {
            'mean': float(np.mean(values)),
            'median': float(np.median(values)),
            'min': float(np.min(values)),
            'max': float(np.max(values)),
            'std': float(np.std(values)),
            'sum': float(np.sum(values))
        }

    if timestamps:
        summary['time_range'] = {
            'earliest': min(timestamps),
            'latest': max(timestamps)
        }

    return summary


def emergency_summarize(data: Any, max_length: int = 500) -> dict:
    """
    Emergency summarization when data is still too large after other strategies

    Args:
        data: Data to summarize
        max_length: Maximum character length for summary

    Returns:
        Emergency summary
    """
    logger.warning("Applying emergency summarization")

    summary = {
        '_emergency_summary': True,
        '_preprocessing': 'emergency_summarize'
    }

    if isinstance(data, dict):
        summary['type'] = 'object'
        summary['keys'] = list(data.keys())[:10]
        summary['key_count'] = len(data.keys())

        if 'Items' in data:
            summary['item_count'] = len(data['Items']) if isinstance(data['Items'], list) else 0

    elif isinstance(data, list):
        summary['type'] = 'array'
        summary['length'] = len(data)
        summary['first_item'] = str(data[0])[:200] if len(data) > 0 else None

    else:
        summary['type'] = 'primitive'
        summary['value'] = str(data)[:max_length]

    return summary


def aggregate_by_field(
    entries: list[dict],
    group_by: str,
    count_field: str | None = None
) -> dict:
    """
    Aggregate entries by a specific field

    Args:
        entries: List of entry objects
        group_by: Field to group by
        count_field: Optional field to count/sum

    Returns:
        Aggregated data grouped by field
    """
    if not entries:
        return {}

    df = pd.DataFrame(entries)

    if group_by not in df.columns:
        logger.warning(f"Field '{group_by}' not found in entries")
        return {}

    if count_field and count_field in df.columns:
        # Aggregate with sum/count of specific field
        grouped = df.groupby(group_by)[count_field].agg(['count', 'sum']).to_dict('index')
    else:
        # Just count occurrences
        grouped = df[group_by].value_counts().to_dict()

    return {
        'grouped_by': group_by,
        'groups': grouped,
        'total_groups': len(grouped),
        '_preprocessing': 'aggregate_by_field'
    }
